(function(w, appRootElement) {

  // Use ngrok to host the application: ngrok http http://localhost:4000
  
  const clientId = 'e8380b1d-e649-4407-af50-31b4c9eddbeb';
  const clientSecret = 'T1H8Q~4n8N3h3eLVbDqWDp35UU8Y3SnX3dyvDcbo';  // not the best idea
  const scope = 'user.read';

  const msalConfig = {
    auth: {
      clientId: clientId,
      authority: clientSecret,
      redirectUri: window.location.origin
    },
    cache: {
      cacheLocation: "localStorage",
      storeAuthStateInCookie: false
    }
  };
  const msalInstance = new msal.PublicClientApplication(msalConfig);

  microsoftTeams.app.initialize().then(() => {
    microsoftTeams.app.getContext().then(async (context) => {
      const user = msalInstance.getAccountByUsername(context.user.userPrincipalName);
      const loggedIn = user ? true : false;
      
      if(!loggedIn) {
        const p = document.createElement('p');
        p.textContent = 'Trying to login ...';
        appRootElement.appendChild(p);

        microsoftTeams.authentication.authenticate({
          url: window.location.origin + `/auth-start.html?clientId=${clientId}&scope=${scope}`,
          successCallback: function (result) {
            console.log('login ok: ' + result);
            window.location.reload();
          },
          failureCallback: function (reason) {
            console.log('error auth: ' + reason);

            const errorP = document.createElement('p');
            errorP.textContent = `Error: ${reason}`;
            appRootElement.appendChild(errorP);
          }
        });
      }
      else {
        // render username
        const p = document.createElement('p');
        p.textContent = `Hello ${user.name} (${user.username})`;
        appRootElement.appendChild(p);
      }
    });
  });
})(window, document.getElementById("root"));