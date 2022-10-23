import { useContext, useState, useCallback, useEffect } from "react";
import { Image } from "@fluentui/react-northstar";
import "./Welcome.css";
import { CurrentUser } from "./CurrentUser";
import { useData } from "@microsoft/teamsfx-react";
import { TeamsFxContext } from "../Context";
import { teams, app } from "@microsoft/teams-js";
import { Mail } from "../../models/mails";

export function Welcome(props: { environment?: string }) {
  const { environment } = {
    environment: window.location.hostname === "localhost" ? "local" : "azure",
    ...props,
  };

  const [ssoToken, setSsoToken] = useState<string>();
  const [tenantId, setTenantId] = useState<string>();
  const [mails, setMails] = useState<Mail[]>()

  const { teamsfx } = useContext(TeamsFxContext);
  const { loading, data, error } = useData(async () => {
    if (teamsfx) {
      const userInfo = await teamsfx.getUserInfo();

      // get creds
      const creds = teamsfx.getCredential() as any;
      const ssoToken = await creds.getSSOToken();
      setSsoToken(ssoToken.token);

      // get tenant id
      await app.initialize()
      var appContext = await app.getContext();
      const tid = appContext.user?.tenant?.id;
      setTenantId(tid);
      
      return userInfo;
    }
  });

  const dataFromServer = useCallback(async () => {
    // use hardcoded url of dotnet project
    const response = await fetch(`http://localhost:5028/token`, {
      method: 'POST',
      cache: 'no-cache',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        tenantId: tenantId, // get tenant id
        ssoIdToken: ssoToken
      })
    });
    const responsePayload = await response.json();
    setMails(responsePayload)

  }, [ssoToken, tenantId, teamsfx]);

  useEffect(() => {
    // if sso token is defined
    if(ssoToken && ssoToken.length > 0 &&
      tenantId && tenantId.length > 0) {
        dataFromServer();
      }
  }, [dataFromServer, ssoToken, tenantId]);

  const userName = (loading || error) ? "": data!.displayName;
  return (
    <div className="welcome page">
      <div className="narrow page-padding">
        <Image src="hello.png" />
        <h1 className="center">Congratulations{userName ? ", " + userName : ""}!</h1>
        <div className="sections">
          <div>
            <CurrentUser userName={userName} />
            { mails && mails.map((mail, i) => {
              return <div><span>{i}.&nbsp;</span><span>{mail.subject}</span> - <span>From: {mail.sender.emailAddress.name} ({mail.sender.emailAddress.address})</span></div>
            })
            }
          </div>
        </div>
      </div>
    </div>
  );
}
