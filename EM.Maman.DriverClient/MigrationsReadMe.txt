whenever the Database of the project is updated, the following command should be executed in the Package Manager Console:
delete the current migration folder and run this command:
Add-Migration InitialCreate -verbose -Project "EM.Maman.Models" -StartupProject "EM.Maman.DriverClient"
then run this command:
Update-Database -verbose -Project "EM.Maman.Models" -StartupProject "EM.Maman.DriverClient"