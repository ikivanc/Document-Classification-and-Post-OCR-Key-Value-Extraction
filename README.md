# Document Classification and Post-OCR Key-Value Extraction

This sample shows how to extract key-value pairs from multiple templates using `Document Classification` and `Key-Value Extraction`.

This an Azure Function sample that accepts Form Image url inputs from the user and extract needed information into a json output. You can use structured and semi-structured Forms to Extract details.

As a test dataset, JFK Files documents are used to classify and extract key-value pairs.

This application has been created using Microsoft Azure Functions, Micosoft Custom Vision AI and Cognitive Services Computer Vision OCR API.

Document classification for JFK files and Post-OCR key-value extraction from froms.

![](Images/Architecture.png)

This sample shows how to extract values from multiple form templates. You can find below flow for multiple documents.

![](Images/Flow.png)


## Custom Vision for Document Classification

For document classification, [Custom Vision Services](https://www.customvision.ai) is used to classify document types. In this sample we have 2 sample documents and these using partly different templates. [Custom Vision AI](https://www.customvision.ai) is very good at classifing these kind of documents.

Here's a screenshot from the portal below.

![](Images/Custom_Vision.png)

## SAS Access token for private Azure Blob Storage Containers

A shared access signature (SAS) provides you with a way to grant limited access to objects in your storage account to other clients, without exposing your account key. For more details about
[Using shared access signatures (SAS)](https://docs.microsoft.com/en-us/azure/storage/common/storage-dotnet-shared-access-signature-part-1)


## Run Project with Local Settings

To run this sample succesfully in your local, First you need to create a file in root called `local.settings.json` and values should be like below.

```
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "AzureWebJobsDashboard": "UseDevelopmentStorage=true",
    "CognitiveServicesUrlBase": "https://westeurope.api.cognitive.microsoft.com/vision/v2.0/",
    "CognitiveServicesKey": "YOUR_COGNITIVE_SERVICES_KEY",
    "ConnectionString": "YOUR_CONNECTION_STRING",
    "ContainerName": "jfkfiles",
    "CustomVisionUrlBase": "https://westeurope.api.cognitive.microsoft.com/customvision/v3.0/Prediction/YOUR_APP_ID/classify/iterations/YOUR_ITERATION_NAME/url",
    "CustomVisionPredictionKey": "YOUR_CUSTOM_VISION_PREDICTION_KEY"
  }
}
```


## Post-OCR Processing: Defining Reference Text (Key) and Desired Value Margins

This technique uses location of bounding boxes returned by Microsoft Cognitive Services OCR API. Region of the key reference texts are defined in a JSON file. Search data notation is like below format:

```
{
  "id": 2,
  "text": "PRIORITY",   // Your Reference Text Value
  "marginX": -30,       // Margin to left of your value field
  "marginY": -30,       // Margin to left top your value field
  "width": 100,         // Width of your text area
  "height": 100         // Height of your text area
}
```

The output regions for above definition will be like below

![](Images/JFK2Values.png)

---
## Extract Key-Values from Mixed Structured Content

Let's use one of the files from JFK Files like below we're targeting to extract 
`CLASSIFIED MESSAGE` , `DEFERRED` , `PRIORITY`, `DTG`, `INCOMING NUMBER` and  `DATE` values. 

![](Images/JFK2.png)

JSON fields for regions will be like below.
This file is located under `Resources` > `ClassifiedMessages.json`

```json
[
  {
    "id": 0,
    "text": "CLASSIFIED MESSAGE",
    "marginX": 5,
    "marginY": -30,
    "width": 200,
    "height": 120
  },
  {
    "id": 1,
    "text": "DEFERRED",
    "marginX": -30,
    "marginY": -30,
    "width": 100,
    "height": 100
  },
  {
    "id": 2,
    "text": "PRIORITY",
    "marginX": -30,
    "marginY": -30,
    "width": 100,
    "height": 100
  },
  {
    "id": 3,
    "text": "DTG",
    "marginX": 5,
    "marginY": -30,
    "width": 200,
    "height": 200
  },
  {
    "id": 4,
    "text": "INCOMING NUMBER",
    "marginX": 0,
    "marginY": -30,
    "width": 200,
    "height": 100
  },
  {
    "id": 5,
    "text": "DATE",
    "marginX": 50,
    "marginY": 20,
    "width": 300,
    "height": 50
  }
]
```
After above definitions search regions will be set like below

![](Images/JFK2Values.png)

And after that, we'll be succesfully extract like below. 

![](Images/JFK2output.png) 


## Extract Key-Values from Semi-Structured Content

Let's use one of the files from JFK Files like below we're targeting to extract 
`FROM` , `TITLE` , `AGENCY ORIGINATOR`, `RECORD NUMBER`, `RECORD SERIES` and  `AGENCY FILE NUMBER` values. 


![](Images/JFK1.png)
 
JSON fields for regions will be like below.

This file is located under `Resources` > `JFKIdentificationForm.json`

```json
[
  {
    "id": 0,
    "text": "RECORD NUMBER",
    "marginX": 220,
    "marginY": 5,
    "width": 300,
    "height": 25
  },
  {
    "id": 1,
    "text": "RECORD SERIES",
    "marginX": 220,
    "marginY": 5,
    "width": 300,
    "height": 20
  },
  {
    "id": 2,
    "text": "AGENCY FILE NUMBER",
    "marginX": 300,
    "marginY": 5,
    "width": 300,
    "height": 25
  },
  {
    "id": 3,
    "text": "AGENCY ORIGINATOR",
    "marginX": 200,
    "marginY": 5,
    "width": 150,
    "height": 25
  },
    {
    "id": 4,
    "text": "FROM",
    "marginX": 50,
    "marginY": 5,
    "width": 150,
    "height": 25
  },
 {
    "id": 5,
    "text": "TITLE",
    "marginX": 50,
    "marginY": 5,
    "width": 600,
    "height": 25
  }
]
```

After above definitions search regions will be set like below
![](Images/JFK1Values.png)

And after that, we'll be succesfully extract like below.

![](Images/JFK1output.png)
