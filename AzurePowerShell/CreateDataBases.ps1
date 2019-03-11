#
# CreateDataBases.ps1
# Source: https://docs.microsoft.com/en-us/azure/sql-database/scripts/sql-database-create-and-configure-database-powershell


# Connect-AzAccount
# The SubscriptionId in which to create these objects
$SubscriptionId = ''
# Set the resource group name and location for your server
$resourceGroupName = "KinaUnaDemo"
$location = "northeurope" #  "westus2"
# Set an admin login and password for your server
$adminSqlLogin = "kinaunademo"
$password = "kinaunademopassword"
# Set server name - the logical server name has to be unique in the system
$serverName = "kinaunademo"
# The sample database name
$authDatabaseName = "kinaunademoauth"
$mediaDatabaseName = "kinaunademomedia"
$progenyDatabaseName = "kinaunademoprogeny"
# The ip address range that you want to allow to access your server
$startIp = "0.0.0.0"
$endIp = "0.0.0.0"

# Set subscription 
Set-AzContext -SubscriptionId $subscriptionId 

# If resource group doesn't exist, create a resource group
Get-AzureRmResourceGroup -Name $resourceGroupName -ErrorVariable notPresent -ErrorAction SilentlyContinue
if ($notPresent)
{
    # ResourceGroup doesn't exist, create it
	Write-Verbose "Resource group doesn't exists, creating group."
	$resourceGroup = New-AzResourceGroup -Name $resourceGroupName -Location $location
}
else{
    Write-Verbose "Resource group already exists."
}

# If the SQL server does not exist, create a server with a system wide unique server name
$serverInstance = Get-AzureRmSqlServer -ServerName $serverName -ResourceGroupName $resourceGroupname -ErrorAction SilentlyContinue

if ($serverInstance) {
    Write-Verbose "SQL server already exists."
	}
else {
    Write-Verbose "SQL server doesn't exists, creating new server."
    $server = New-AzSqlServer -ResourceGroupName $resourceGroupName `
    -ServerName $serverName `
    -Location $location `
    -SqlAdministratorCredentials $(New-Object -TypeName System.Management.Automation.PSCredential -ArgumentList $adminSqlLogin, $(ConvertTo-SecureString -String $password -AsPlainText -Force))
	}



# Create a server firewall rule that allows access from the specified IP range
$serverFirewallRule = New-AzSqlServerFirewallRule -ResourceGroupName $resourceGroupName `
    -ServerName $serverName `
    -FirewallRuleName "AllowedKinaUnaDemoIPs" -StartIpAddress $startIp -EndIpAddress $endIp

# Create a blank database with an S0 performance level
$authDatabase = New-AzSqlDatabase  -ResourceGroupName $resourceGroupName `
    -ServerName $serverName `
    -DatabaseName $authDatabaseName `
    -RequestedServiceObjectiveName "S0" `

# Create a blank database with an S0 performance level
$mediaDatabase = New-AzSqlDatabase  -ResourceGroupName $resourceGroupName `
    -ServerName $serverName `
    -DatabaseName $mediaDatabaseName `
    -RequestedServiceObjectiveName "S0" ` 

# Create a blank database with an S0 performance level
$progenyDatabase = New-AzSqlDatabase  -ResourceGroupName $resourceGroupName `
    -ServerName $serverName `
    -DatabaseName $progenyDatabaseName `
    -RequestedServiceObjectiveName "S0" `

