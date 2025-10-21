# Azure function apps

* The function `HttpToQueue` is triggered by an http request, creates a message in the Message Bus Queue `orders`
* The function `OrderProcessor` is triggered by a new message in the Service Bus Queue `orders`

## Install dev env
```bash
brew install dotnet-sdk 
#require net8.0 in following list
dotnet --list-sdks

brew tap azure/functions
brew install azure-functions-core-tools@4
dotnet new install Microsoft.Azure.Functions.Worker.ProjectTemplates

dotnet restore
```

## Publish/deploy
```bash
dotnet publish -c Release -f net8.0 -o ./publish
cd publish && zip -r ../deploy.zip . && cd ..
az functionapp deployment source config-zip -g $resourcegroup -n $appname --src deploy.zip
```
## Get function app key
```bash
KEY=$(az functionapp keys list -g test -n $appname --query functionKeys.default -o tsv)
```
## Run/invoke locally
```bash
func start
curl -L -X POST "http://localhost:7071/api/HttpToQueue?code=$KEY -d 'hello from cloud' --verbose"
```
## Add config settings
```bash
az functionapp config appsettings set \
    -g test -n $appname \
    --settings "<ServiceBusConnectionString-from-entity-shared-policy>"
az functionapp config appsettings set \
    -g test -n $appname \
    --settings "APPINSIGHTS_CONNECTIONSTRING=<AppInsightsConnectionString>"
```
## Run/invoke in Azure
```bash
curl -L -X POST "https://${appname}.azurewebsites.net/api/HttpToQueue" -d "hello from cloud" --verbose -H "x-functions-key: $KEY"
```