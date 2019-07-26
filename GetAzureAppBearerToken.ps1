$clientId = ''
$clientSecret = ''
$tenant = ''

Add-Type -Path 'C:\Program Files\WindowsPowerShell\Modules\Azure\**\Services\Microsoft.IdentityModel.Clients.ActiveDirectory.dll'
$credentials = [Microsoft.IdentityModel.Clients.ActiveDirectory.ClientCredential]::new($clientId, $clientSecret)
$authority = 'https://login.microsoftonline.com/' + $tenant + '/oauth2/token'
$authContext = [Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext]::new($authority)
$authResult = $authContext.AcquireTokenAsync($clientId, $credentials)
$authResult.Result.AccessToken