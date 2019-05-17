# Goals
- use AsyncLocal<Activity> to setup correlation
- make sure call graph is represented in app insights

# Steps
1. create AppInsights, KV, SPN used to access KV, instrumentation key of AppInsights is stored in KV, Cert is downloaded and used to authenticate SPN
``` PowerShell
.\BootstrapKeyVaultAccess.ps1
```
2. Run the application
3. Explore logs in azure portal --> application insights --> search 
![distributed trace](app-insights-trace.png)