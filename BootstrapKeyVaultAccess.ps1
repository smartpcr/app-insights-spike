param(
    [string] $SubscriptionName = "Compliance_Tools_Eng",
    [string] $VaultName = "xiaodong-kv",
    [string] $SpName = "kv-reader-xiaodong",
    [string] $VaultResourceGroupName = "azds-rg"
)

function LoginToAzure() {
    param([string] $SubscriptionName)

    $azAccount = az account show | ConvertFrom-Json
    if ($null -eq $azAccount -or $azAccount.name -ine $SubscriptionName) {
        az login | Out-Null
        az account set --subscription $SubscriptionName | Out-Null
    }

    $currentAccount = az account show | ConvertFrom-Json
    return $currentAccount
}

function EnsureCertificateInKeyVault {
    param(
        [string] $VaultName,
        [string] $CertName
    )

    $existingCert = az keyvault certificate list --vault-name $VaultName --query "[?id=='https://$VaultName.vault.azure.net/certificates/$CertName']" | ConvertFrom-Json
    if (!$existingCert) {
        $credentialFolder = Join-Path $env:USERPROFILE ".secrets"
        if (-not (Test-Path $credentialFolder)) {
            New-Item -Path $credentialFolder -ItemType Directory -Force | Out-Null
        }
        $defaultPolicyFile = Join-Path $credentialFolder "default_policy.json"
        az keyvault certificate get-default-policy -o json | Out-File $defaultPolicyFile -Encoding utf8 
        az keyvault certificate create -n $CertName --vault-name $VaultName -p @$defaultPolicyFile | Out-Null
    }
}

function DownloadCertFromKeyVault {
    param(
        [string] $VaultName,
        [string] $CertName
    )

    $credentialFolder = Join-Path $env:USERPROFILE ".secrets"
    if (-not (Test-Path $credentialFolder)) {
        New-Item -Path $credentialFolder -ItemType Directory -Force | Out-Null
    }
    $pfxCertFile = Join-Path $credentialFolder "$certName.pfx"
    $pemCertFile = Join-Path $credentialFolder "$certName.pem"
    $keyCertFile = Join-Path $credentialFolder "$certName.key"

    az keyvault secret download --vault-name $VaultName -n $CertName -e base64 -f $pfxCertFile
    openssl pkcs12 -in $pfxCertFile -clcerts -nodes -out $keyCertFile -passin pass:
    openssl rsa -in $keyCertFile -out $pemCertFile | Out-Null
}

function EnsureServicePrincipal() {
    param(
        [string] $SpName,
        [string] $VaultName,
        [string] $VaultResourceGroupName
    )

    $sp = az ad sp list --display-name $SpName | ConvertFrom-Json
    if (!$sp) {
        $certName = "$SpName-cert"
        EnsureCertificateInKeyVault -VaultName $VaultName -CertName $certName 
        
        az ad sp create-for-rbac -n $SpName --role contributor --keyvault $VaultName --cert $certName | Out-Null
        $sp = az ad sp list --display-name $SpName | ConvertFrom-Json
    
        az keyvault set-policy `
            --name $VaultName `
            --resource-group $VaultResourceGroupName `
            --object-id $sp.objectId `
            --spn $sp.displayName `
            --certificate-permissions get list update delete `
            --secret-permissions get list set delete | Out-Null
    }

    $sp = az ad sp list --display-name $SpName | ConvertFrom-Json
    return $sp 
}

function InstallCert() {
    param(
        [string] $VaultName,
        [string] $CertName
    )

    $credentialFolder = Join-Path $env:USERPROFILE ".secrets"
    if (-not (Test-Path $credentialFolder)) {
        New-Item -Path $credentialFolder -ItemType Directory -Force | Out-Null
    }
    $pfxCertFile = Join-Path $credentialFolder "$certName.pfx"
    if (Test-Path $pfxCertFile) {
        Remove-Item $pfxCertFile -Force | Out-Null
    }

    az keyvault secret download --vault-name $VaultName -n $CertName -e base64 -f $pfxCertFile 
    $pfxBytes = [System.IO.File]::ReadAllBytes($pfxCertFile)
    $flags = [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::MachineKeySet -bxor [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::PersistKeySet
    $pfx = new-object System.Security.Cryptography.X509Certificates.X509Certificate2
    $pfx.Import($pfxBytes, "", $flags)
    $thumbprint = $pfx.Thumbprint

    $certAlreadyExists = Test-Path Cert:\CurrentUser\My\$thumbprint
    if (!$certAlreadyExists) {
        $x509Store = new-object System.Security.Cryptography.X509Certificates.X509Store -ArgumentList My, CurrentUser
        $x509Store.Open('ReadWrite')
        $x509Store.Add($pfx)
    }

    return $thumbprint
}

LoginToAzure -SubscriptionName $SubscriptionName | Out-Null
$sp = EnsureServicePrincipal -SpName $SpName -VaultName $VaultName -VaultResourceGroupName $VaultResourceGroupName 
$thumbprint = InstallCert -VaultName $VaultName -CertName "$SpName-cert"

Write-Host "Copy thumbprint to appsettings.json file: `n" -ForegroundColor Yellow
$settings = @{
    clientId             = $sp.appId 
    clientCertThumbprint = $thumbprint
    clientCertFile       = "$env:USERPROFILE\$spName-cert.pfx"
} | ConvertTo-Json

Write-Host $settings -ForegroundColor Green
Write-Host "`n"