# Deployment script for Resource Group Creator Azure Function

# Parameters
param(
    [Parameter(Mandatory=$true)]
    [string]$SubscriptionId,
    
    [Parameter(Mandatory=$true)]
    [string]$DeploymentResourceGroup,
    
    [Parameter(Mandatory=$true)]
    [string]$Location,
    
    [Parameter(Mandatory=$true)]
    [string]$FunctionAppName
)

# Login to Azure (if not already logged in)
$context = Get-AzContext
if (!$context) {
    Connect-AzAccount
    $context = Get-AzContext
}

# Set subscription
Set-AzContext -SubscriptionId $SubscriptionId

# Create resource group for deployment if it doesn't exist
$rg = Get-AzResourceGroup -Name $DeploymentResourceGroup -ErrorAction SilentlyContinue
if (!$rg) {
    New-AzResourceGroup -Name $DeploymentResourceGroup -Location $Location
    Write-Host "Resource group $DeploymentResourceGroup created."
}

# Deploy ARM template
$templateFile = "azure-function-arm-template.json"
$deploymentName = "ResourceGroupCreator-" + (Get-Date).ToString("yyyyMMdd-HHmmss")
$parameters = @{
    functionAppName = $FunctionAppName
    location = $Location
}

Write-Host "Deploying ARM template..."
New-AzResourceGroupDeployment `
    -Name $deploymentName `
    -ResourceGroupName $DeploymentResourceGroup `
    -TemplateFile $templateFile `
    -TemplateParameterObject $parameters

# Configure the Function App with the subscription ID
Write-Host "Setting up application settings..."
$app = Get-AzFunctionApp -Name $FunctionAppName -ResourceGroupName $DeploymentResourceGroup
Update-AzFunctionAppSetting -Name $FunctionAppName -ResourceGroupName $DeploymentResourceGroup -AppSetting @{"SUBSCRIPTION_ID" = $SubscriptionId}

# Get the function URL with key
$keys = Get-AzFunctionAppHostKey -Name $FunctionAppName -ResourceGroupName $DeploymentResourceGroup
$functionUrl = "https://$FunctionAppName.azurewebsites.net/api/CreateResourceGroup?code=$($keys.default)"

Write-Host "Deployment completed successfully!"
Write-Host "Function URL: $functionUrl"
Write-Host ""
Write-Host "To use the function, send a POST request with the following JSON body:"
Write-Host '{
    "resourceGroupName": "YourNewResourceGroupName",
    "location": "eastus"
}'