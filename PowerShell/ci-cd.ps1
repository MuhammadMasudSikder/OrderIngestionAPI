$ErrorActionPreference = "Stop"

# -------------------------------
# Configuration
# -------------------------------
$IMAGE_NAME = "orderingestion-api"
$NAMESPACE  = "orderingestion-api-prod"
$IMAGE_TAG  = "latest"
$INFRA_DIR  = "./infra"
$API_PROJ   = "./OrderIngestionAPI/OrderIngestionAPI.csproj"
$DOCKERFILE = "./OrderIngestionAPI/Dockerfile"
$K8S_MANIFEST = "./k8s/self-signed-deployment.yaml"

# -------------------------------
# Azure Login
# -------------------------------
Write-Host "Logging into Azure..."
az login --service-principal `
  -u $env:AZURE_CLIENT_ID `
  -p $env:AZURE_CLIENT_SECRET `
  --tenant $env:AZURE_TENANT_ID | Out-Null

az account set --subscription $env:AZURE_SUBSCRIPTION_ID

# -------------------------------
# Terraform (IaC)
# -------------------------------
Write-Host "Running Terraform..."
Push-Location $INFRA_DIR

terraform init
terraform validate
terraform plan -out=tfplan
terraform apply -auto-approve tfplan

Pop-Location

# -------------------------------
# .NET Restore, Build, Test
# -------------------------------
Write-Host "Restoring dependencies..."
dotnet restore $API_PROJ

Write-Host "Building application..."
dotnet build $API_PROJ -c Release --no-restore

Write-Host "Running unit tests..."
dotnet test -c Release --no-build --collect:"XPlat Code Coverage"


# -------------------------------
# C# Linting (dotnet format)
# -------------------------------
Write-Host "Running C# linting (dotnet format)..."
try {
    dotnet format --verify-no-changes
}
catch {
    Write-Warning "C# linting failed, continuing pipeline..."
}


# -------------------------------
# Dockerfile Lint (Hadolint)
# -------------------------------
Write-Host "Linting Dockerfile..."
docker run --rm -i hadolint/hadolint < $DOCKERFILE


# -------------------------------
# Kubernetes YAML Linting
# -------------------------------
Write-Host "Linting Kubernetes YAML files..."

$YAML_DIR = "./k8s"
$YAMLLINT_CONFIG = "./.yamllint.yml"

if (Test-Path $YAMLLINT_CONFIG) {
    docker run --rm `
      -v "${PWD}:/workdir" `
      cytopia/yamllint `
      -c /workdir/.yamllint.yml `
      /workdir/k8s
}
else {
    docker run --rm `
      -v "${PWD}:/workdir" `
      cytopia/yamllint `
      /workdir/k8s
}


# -------------------------------
# Docker Build & Push
# -------------------------------
Write-Host "Logging into ACR..."
$env:ACR_PASSWORD | docker login $env:ACR_LOGIN_SERVER `
  -u $env:ACR_USERNAME `
  --password-stdin

Write-Host "Building Docker image..."
docker build `
  -t "$($env:ACR_LOGIN_SERVER)/$IMAGE_NAME:$IMAGE_TAG" `
  -f $DOCKERFILE .

Write-Host "Pushing Docker image..."
docker push "$($env:ACR_LOGIN_SERVER)/$IMAGE_NAME:$IMAGE_TAG"

# -------------------------------
# AKS Context
# -------------------------------
Write-Host "Connecting to AKS..."
az aks get-credentials `
  --resource-group $env:AKS_RESOURCE_GROUP `
  --name $env:AKS_CLUSTER_NAME `
  --overwrite-existing

# -------------------------------
# Kubernetes Deployment
# -------------------------------
Write-Host "Ensuring namespace exists..."
kubectl get namespace $NAMESPACE `
  || kubectl create namespace $NAMESPACE

Write-Host "Updating deployment image..."
kubectl set image deployment/$IMAGE_NAME `
  $IMAGE_NAME="$($env:ACR_LOGIN_SERVER)/$IMAGE_NAME:$IMAGE_TAG" `
  -n $NAMESPACE

Write-Host "Applying Kubernetes manifest..."
kubectl apply -f $K8S_MANIFEST -n $NAMESPACE

Write-Host "Restarting deployment..."
kubectl rollout restart deployment/$IMAGE_NAME -n $NAMESPACE

Write-Host "Checking rollout status..."
kubectl rollout status deployment/$IMAGE_NAME -n $NAMESPACE

Write-Host "CI/CD pipeline completed successfully."
