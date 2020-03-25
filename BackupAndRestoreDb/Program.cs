using System;
using System.Configuration;
using System.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace BackupAndRestoreDb
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var connectionString = ConfigurationManager.ConnectionStrings["Database"].ConnectionString;

                var backupDate = DateTime.Now;
                var backupFileName = $"Northwind_{backupDate:yyyy-MM-dd-HH-mm-ss}.bak";

                BackupDatabase(connectionString, backupFileName);
                RestoreDatabase(connectionString, backupFileName);

                // Remove the backup files from the hard disk.  
                // This location is dependent on the installation of SQL Server  
                System.IO.File.Delete($"C:\\Program Files\\Microsoft SQL Server\\MSSQL13.MSSQLSERVER\\MSSQL\\Backup\\{backupFileName}");

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Success!");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex);
                Console.ResetColor();
            }

            Console.WriteLine("Done");
            Console.ReadLine();
        }

        private static void BackupDatabase(string connectionString, string backupFileName)
        {
            using (var sqlConnection = new SqlConnection(connectionString))
            {
                // Connect to the instance of SQL Server.   
                Server srv = new Server(new ServerConnection(sqlConnection));
                // Define a Backup object variable.   
                Backup bk = new Backup();

                // Specify the type of backup, the description, the name, and the database to be backed up.   
                bk.Action = BackupActionType.Database;
                bk.BackupSetDescription = "Full backup of Northwind";
                bk.BackupSetName = "Northwind Backup";
                bk.Database = "Northwind";

                // Declare a BackupDeviceItem by supplying the backup device file name in the constructor, and the type of device is a file. 
                var bdi = new BackupDeviceItem(backupFileName, DeviceType.File);

                // Add the device to the Backup object.   
                bk.Devices.Add(bdi);
                // Set the Incremental property to False to specify that this is a full database backup.   
                bk.Incremental = false;

                // Set the expiration date.   
                bk.ExpirationDate = DateTime.Today.AddYears(10);

                // Specify that the log must be truncated after the backup is complete.   
                bk.LogTruncation = BackupTruncateLogType.Truncate;

                // Run SqlBackup to perform the full database backup on the instance of SQL Server.   
                bk.SqlBackup(srv);

                // Inform the user that the backup has been completed.   
                Console.WriteLine("Full Backup complete.");

                // Remove the backup device from the Backup object.   
                bk.Devices.Remove(bdi);
            }
        }

        private static void RestoreDatabase(string connectionString, string backupFileName)
        {
            using (var sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();

                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandType = System.Data.CommandType.Text;
                    sqlCommand.CommandText = "USE master; " +
                                             "ALTER DATABASE [Northwind] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;";

                    sqlCommand.ExecuteNonQuery();
                }

                try
                {
                    using (var sqlCommand = sqlConnection.CreateCommand())
                    {
                        sqlCommand.CommandType = System.Data.CommandType.Text;
                        sqlCommand.CommandText = $"RESTORE DATABASE [Northwind] FROM DISK = '{backupFileName}' WITH REPLACE";

                        sqlCommand.ExecuteNonQuery();
                    }
                }
                finally
                {
                    using (var sqlCommand = sqlConnection.CreateCommand())
                    {
                        sqlCommand.CommandType = System.Data.CommandType.Text;
                        sqlCommand.CommandText = "ALTER DATABASE [Northwind] SET MULTI_USER;";

                        sqlCommand.ExecuteNonQuery();
                    }
                }

                // Inform the user that the Full Database Restore is complete.   
                Console.WriteLine("Full Database Restore complete.");
            }
        }

        private static void BackupAndRestoreDatabase()
        {
            try
            {
                var connectionString = ConfigurationManager.ConnectionStrings["Database"].ConnectionString;
                var sqlConnection = new SqlConnection(connectionString);

                // Connect to the instance of SQL Server.   
                Server srv = new Server(new ServerConnection(sqlConnection));

                // Reference the Northwind database.   
                Database db = default(Database);
                db = srv.Databases["Northwind"];

                // Store the current recovery model in a variable.   
                int recoverymod = (int)db.DatabaseOptions.RecoveryModel;

                // Define a Backup object variable.   
                Backup bk = new Backup();

                // Specify the type of backup, the description, the name, and the database to be backed up.   
                bk.Action = BackupActionType.Database;
                bk.BackupSetDescription = "Full backup of Northwind";
                bk.BackupSetName = "Northwind Backup";
                bk.Database = "Northwind";

                // Declare a BackupDeviceItem by supplying the backup device file name in the constructor, and the type of device is a file.   
                BackupDeviceItem bdi = default(BackupDeviceItem);
                bdi = new BackupDeviceItem("Test_Full_Backup1", DeviceType.File);

                // Add the device to the Backup object.   
                bk.Devices.Add(bdi);
                // Set the Incremental property to False to specify that this is a full database backup.   
                bk.Incremental = false;

                // Set the expiration date.   
                DateTime backupdate = new DateTime();
                backupdate = new DateTime(2006, 10, 5);
                bk.ExpirationDate = backupdate;

                // Specify that the log must be truncated after the backup is complete.   
                bk.LogTruncation = BackupTruncateLogType.Truncate;

                // Run SqlBackup to perform the full database backup on the instance of SQL Server.   
                bk.SqlBackup(srv);

                // Inform the user that the backup has been completed.   
                Console.WriteLine("Full Backup complete.");

                // Remove the backup device from the Backup object.   
                bk.Devices.Remove(bdi);

                // Create another file device for the differential backup and add the Backup object.   
                BackupDeviceItem bdid = default(BackupDeviceItem);
                bdid = new BackupDeviceItem("Test_Differential_Backup1", DeviceType.File);

                // Add the device to the Backup object.   
                bk.Devices.Add(bdid);

                // Set the Incremental property to True for a differential backup.   
                bk.Incremental = true;

                // Run SqlBackup to perform the incremental database backup on the instance of SQL Server.   
                bk.SqlBackup(srv);

                // Inform the user that the differential backup is complete.   
                Console.WriteLine("Differential Backup complete.");

                // Remove the device from the Backup object.   
                bk.Devices.Remove(bdid);

                // Delete the Northwind database before restoring it  
                db.Drop();

                // Define a Restore object variable.  
                Restore rs = new Restore();

                // Set the NoRecovery property to true, so the transactions are not recovered.   
                rs.NoRecovery = true;

                // Add the device that contains the full database backup to the Restore object.   
                rs.Devices.Add(bdi);

                // Specify the database name.   
                rs.Database = "Northwind";

                // Restore the full database backup with no recovery.   
                rs.SqlRestore(srv);

                // Inform the user that the Full Database Restore is complete.   
                Console.WriteLine("Full Database Restore complete.");

                // reacquire a reference to the database  
                db = srv.Databases["Northwind"];

                // Remove the device from the Restore object.  
                rs.Devices.Remove(bdi);

                // Set the NoRecovery property to False.   
                rs.NoRecovery = false;

                // Add the device that contains the differential backup to the Restore object.   
                rs.Devices.Add(bdid);

                // Restore the differential database backup with recovery.   
                rs.SqlRestore(srv);

                // Inform the user that the differential database restore is complete.   
                Console.WriteLine("Differential Database Restore complete.");

                // Remove the device.   
                rs.Devices.Remove(bdid);

                // Set the database recovery mode back to its original value.  
                db.RecoveryModel = (RecoveryModel)recoverymod;

                // Remove the backup files from the hard disk.  
                // This location is dependent on the installation of SQL Server  
                System.IO.File.Delete("C:\\Program Files\\Microsoft SQL Server\\MSSQL13.MSSQLSERVER\\MSSQL\\Backup\\Test_Full_Backup1");
                System.IO.File.Delete("C:\\Program Files\\Microsoft SQL Server\\MSSQL13.MSSQLSERVER\\MSSQL\\Backup\\Test_Differential_Backup1");

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Success!");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex);
                Console.ResetColor();
            }
        }
    }
}