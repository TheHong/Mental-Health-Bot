const styleOptions = {
    botAvatarImage: 'img/bot-img.png',
    userAvatarImage: 'img/user-img.png',
    botAvatarInitials: 'MH',
    userAvatarInitials: 'WC',
    bubbleBackground: 'rgba(0, 0, 255, .1)',
    bubbleFromUserBackground: 'rgba(0, 255, 0, .1)',
    // rootHeight: '50%',
    // rootWidth: '50%'
};

window.WebChat.renderWebChat(
{
   directLine: window.WebChat.createDirectLine({
      secret: getBotSecret() // Comes from userInfo.js
   }),
   styleOptions
},
document.getElementById('webchat')
);

// Method 3: Get token via request
// var xhr = new XMLHttpRequest();
// xhr.open('GET', "https://webchat.botframework.com/api/tokens", true);
// xhr.setRequestHeader('Authorization', 'BotConnector ' + getBotSecret());
// xhr.send();
// xhr.onreadystatechange = processRequest;

// function processRequest(e) {
//   if (xhr.readyState == 4  && xhr.status == 200) {
//     var response = JSON.parse(xhr.responseText);
//     // document.getElementById("chat").src="https://webchat.botframework.com/embed/MHBot?t="+response
//     window.WebChat.renderWebChat(
//         {
//            directLine: window.WebChat.createDirectLine({
//               token: response
//            }),
//            userID: 'YOUR_USER_ID',
//            username: 'Web Chat User',
//            locale: 'en-US',
//            botAvatarInitials: 'WC',
//            userAvatarInitials: 'WW'
//         },
//         document.getElementById('webchat')
//      );
//   }
// }