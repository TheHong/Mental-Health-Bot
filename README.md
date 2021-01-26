# Mental-Health-Bot

![Short demo of some of Felix's core capabilities](Felix-Short-Demo.gif)

## About the Bot

### Introducing Felix
__Felix__ is a bot that aims to improve access to mental health resources. Although the bot currently directs users to mental health resources across University of Toronto (based on \[[0](#sources)\]), the idea could be applied to other institutions as well. It was made using the Microsoft Azure Bot Framework SDK as part of the *Microsoft Discover AI Upskilling Journey Sprint 5 of the Technical Learning Path 2020* (🏆 top 3). 

### Context
With the recent pushes for change in mental health policies at various Canadian universities (such as University of Ottawa, Waterloo, and Toronto), steps have been taken to improve the access and quality of mental health resources. The idea of Felix was created with the purpose of augmenting the already existing mental health resources by helping users find the resources they need and lessen the workload of operators.

### Limitations and Next Steps
This is merely a prototype, hoping to show what a mental health bot could look like. More care and research will be needed to ensure what Felix says to the users is always culturally and situationally sensitive. Currently, Felix does not always say things that it should. Another next step would be to be fluent in other languages. It can currently detect Chinese and French; in the future, it should be able to converse in not only those two languages, but also other languages so that it is as accessible as possible. But most importantly, our experience is limited. In order for a mental health bot to work well, we will have to learn more about the community of end-users and collaborate with them to create a more refined and robust solution. The important part is to not only design *for* the users, but design *with* them.

__Update:__ Shortly after our project, University of Toronto launched similar system (named Navi \[[1](#sources)\]), thus affirming significance of this opportunity.

## Technical Implementation

### Core Capabilities
Felix's core capabilities are as follows:
- Refer users to appropriate mental health resources based on conversation
- Detect urgency in user's conversation and redirect appropriately
- Detect French and Chinese
- Detect user's intent to have the conversation handed over to a human operator
- Chat with the user

### Technologies Used
- Felix was first developed offline using C# and Microsoft's Bot Framework Emulator. 
- We then deployed the bot onto Microsoft Azure's Web App Bot service and used HTML/CSS/JavaScript to embed the bot onto a website. 
- Resources were fetched from a dataset by comparing keywords extracted from the chat (as word vectors) and the resource tags (also as word vectors). This fetching functionality was developed and tested in Python before being implemented in C#. 
- The other core capabilities were accomplished using the following Azure services: Text Analytics, Language Understanding (LUIS), and QnA Maker.

### Running the Code
First step is to create a QnA Maker, LUIS, and Text

For the __bot__, first step is to create a QnA Maker, LUIS, and Text Analytics service on Microsoft Azure. After that, include the necessary information in appsettings.json. The following are some notes:
- For qna:HostName, should be of the form "https://____.azurewebsites.net/qnamaker"
- For luis:APIHostName, should be of the form "https://___.api.cognitive.microsoft.com"
- For textanalytics:Endpoint, should be of the form "https://_____.cognitiveservices.azure.com/"

For the __webpage__, include the necessary information in userInfo.js.

### Example Phrases to Test
Although Felix is not perfect, some demo phrases to try out to illustrate its functionality can be found in Documents/Example_chat.txt.

## <a name="sources"></a>Sources
\[0\] https://studentlife.utoronto.ca/wp-content/uploads/Feeling-distressed.pdf

\[1\] uoft.me/navi
