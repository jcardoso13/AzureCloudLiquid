# CloudLiquid Azure Function

CloudLiquid Azure Function is a powerful serverless computing solution built on Azure Functions. It provides a flexible, event-driven architecture that enables you to execute your code in response to HTTP requests without managing infrastructure. This Azure Function integrates with Azure Storage and leverages the CloudLiquid library for processing Liquid templates, making it an ideal choice for dynamic content generation.

## Features

- **Serverless Execution**: Runs in an Azure Functions environment, ensuring you only pay for the compute time you consume.
- **Liquid Template Processing**: Utilizes the CloudLiquid library for processing Liquid templates, offering a robust templating engine for your applications.
- **Azure Storage Integration**: Seamlessly integrates with Azure Blob Storage for storing and managing your templates and content.
- **Exception Handling**: Includes a custom middleware for centralized exception handling, improving the reliability of your application.

## Getting Started

### Prerequisites

- Azure Functions Core Tools
- .NET 8.0 SDK
- Azure Storage Account (for Blob Storage) or Azurite
- Visual Studio Code or Visual Studio (recommended for development)

### Setup

1. Clone this repository to your local machine.
2. Navigate to the cloned directory.
3. Ensure you have the Azure Functions Core Tools installed.
4. Set up your Azure Storage account and retrieve your connection string.
5. Update the `local.settings.json` file with your Azure Storage connection string or in VSCode run Azurite in the command pallete. While running the docker-compose, it will automatically start Azurite.

### Running Locally

1. Open a terminal in the project directory.
2. Run the following command to start the Azure Function locally:

    ```sh
    func start
    ```
    Alternatively in Docker

    ```sh
    docker-compose build
    docker-compose up
    ```
3. The function will be available at http://localhost:7071/api/CloudLiquid or http://localhost:8080/api/CloudLiquid if using Docker.


### Deployment

1. Log in to your Azure account using the Azure CLI:
    
    ```sh
    az login
    ```
2. Create a Function App in Azure:
    ```sh
    az functionapp create --resource-group <YourResourceGroup> --consumption-plan-location <Location> --runtime dotnet --functions-version 4 --name <YourFunctionAppName> --storage-account <YourStorageAccountName>
    ```
3. Deploy your function to Azure:
    ```sh
    func azure functionapp publish <YourFunctionAppName>
    ```
    For more information check the official [documentation](https://learn.microsoft.com/en-us/azure/azure-functions/functions-core-tools-reference?tabs=v2).

## Usage

Send a `POST` request to your function's endpoint with a JSON payload containing your Liquid template and data. The function will process the template and return the rendered content.

## Contributing

Contributions are welcome! Please read our contributing guidelines for more information on how to contribute to the CloudLiquid Azure Function project.

## License

This project is licensed under the GNU General Public License v3.0 (GPLv3) - see the [LICENSE](LICENSE) file for details.