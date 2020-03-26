using System;
using System.IO;
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

                var backupDirectory = ConfigurationManager.AppSettings["BackupDirectory"];
                var backupFileName = $"Northwind_{ DateTime.Now:yyyy-MM-dd-HH-mm-ss}.bak";
                var backupFilePath = Path.Combine(backupDirectory, backupFileName);

                BackupDB(backupFilePath, connectionString);
                RestoreDB(backupFilePath, connectionString);

                // Remove the backup files from the hard disk
                File.Delete(backupFilePath);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex);
                Console.ResetColor();
            }

            Console.ReadLine();
        }

        private static void BackupDB(string backupFilePath, string connectionString)
        {
            Console.WriteLine("Backup operation started...");

            // Define a Backup object variable
            Backup backup = new Backup();

            // Set type of backup to be performed to database
            backup.Action = BackupActionType.Database;
            backup.BackupSetDescription = "Full backup of Northwind";
            // Set the name used to identify a particular backup set
            backup.BackupSetName = "Northwind Backup";
            // Specify the name of the database to back up
            backup.Database = "Northwind";

            // Set up the backup device to use filesystem
            BackupDeviceItem deviceItem = new BackupDeviceItem(backupFilePath, DeviceType.File);
            // Add the device to the Backup object
            backup.Devices.Add(deviceItem);

            // Setup a new connection to the data server
            ServerConnection connection = new ServerConnection(new SqlConnection(connectionString));
            Server sqlServer = new Server(connection);

            // Initialize devices associated with a backup operation
            backup.Initialize = true;
            backup.Checksum = true;
            // Set it to true to have the process continue even after checksum error
            backup.ContinueAfterError = true;
            // Set the Incremental property to False to specify that this is a full database backup  
            backup.Incremental = false;
            // Set the backup expiration date
            backup.ExpirationDate = DateTime.Now.AddYears(1);
            // Specify that the log must be truncated after the backup is complete
            backup.LogTruncation = BackupTruncateLogType.Truncate;

            // Run SqlBackup to perform the full database backup on the instance of SQL Server
            backup.SqlBackup(sqlServer);

            // Inform the user that the backup has been completed 
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Backup operation succeeded.");
            Console.ResetColor();
        }

        private static void RestoreDB(string backupFilePath, string connectionString)
        {
            Console.WriteLine("Restore operation started...");

            // Define a Restore object variable
            Restore restore = new Restore();

            // Specify the database name
            restore.Database = "Northwind";
            restore.Action = RestoreActionType.Database;

            // Add the device that contains the full database backup to the Restore object         
            restore.Devices.AddDevice(backupFilePath, DeviceType.File);

            // Set ReplaceDatabase = true to create new database regardless of the existence of specified database
            restore.ReplaceDatabase = true;

            // Set the NoRecovery property to False
            // If you have a differential or log restore to be followed, you would specify NoRecovery = true
            restore.NoRecovery = false;

            // Setup a new connection to the data server
            ServerConnection connection = new ServerConnection(new SqlConnection(connectionString));
            Server sqlServer = new Server(connection);

            // Restore the full database backup with recovery         
            restore.SqlRestore(sqlServer);

            // Inform the user that the restore has been completed
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Restore operation succeeded.");
            Console.ResetColor();
        }
    }
}