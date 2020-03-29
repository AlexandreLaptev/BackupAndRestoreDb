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

            ServerConnection connection = null;

            try
            {
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
                connection = new ServerConnection(new SqlConnection(connectionString));
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

                backup.PercentCompleteNotification = 10;

                backup.PercentComplete += (s, e) =>
                {
                    // Inform the user percent complete
                    Console.WriteLine($"Percent Complete: {e.Percent}");
                };

                // Run SqlBackup to perform the full database backup on the instance of SQL Server
                backup.SqlBackup(sqlServer);

                // Inform the user that the backup has been completed 
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Backup has been completed");
                Console.ResetColor();
            }
            // Catch the SMO exception   
            catch (SmoException smoex)
            {
                // Display the SMO exception message 
                Console.WriteLine(smoex.Message);

                // Display the sequence of non-SMO exceptions that caused the SMO exception 
                var ex = smoex.InnerException;
                while (!ReferenceEquals(ex.InnerException, (null)))
                {
                    Console.WriteLine(ex.InnerException.Message);
                    ex = ex.InnerException;
                }
            }
            // Catch other non-SMO exceptions
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Backup failed: " + ex.StackTrace);
                Console.ResetColor();
            }
            finally
            {
                if (connection != null)
                    connection.Disconnect();
            }
        }

        private static void RestoreDB(string backupFilePath, string connectionString)
        {
            Console.WriteLine("Restore operation started...");

            ServerConnection connection = null;

            try
            {
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

                restore.PercentCompleteNotification = 10;

                // Setup a new connection to the data server
                connection = new ServerConnection(new SqlConnection(connectionString));
                Server sqlServer = new Server(connection);

                restore.PercentComplete += (s, e) =>
                {
                    // Inform the user percent complete
                    Console.WriteLine($"Percent Complete: {e.Percent}");
                };

                // Restore the full database backup with recovery         
                restore.SqlRestore(sqlServer);

                var db = sqlServer.Databases[restore.Database];
                db.SetOnline();
                sqlServer.Refresh();
                db.Refresh();

                // Inform the user that the restore has been completed
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Restore has been completed");
                Console.ResetColor();
            }
            // Catch the SMO exception   
            catch (SmoException smoex)
            {
                // Display the SMO exception message 
                Console.WriteLine(smoex.Message);

                // Display the sequence of non-SMO exceptions that caused the SMO exception 
                var ex = smoex.InnerException;
                while (!ReferenceEquals(ex.InnerException, (null)))
                {
                    Console.WriteLine(ex.InnerException.Message);
                    ex = ex.InnerException;
                }
            }
            // Catch other non-SMO exceptions
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Restore failed: " + ex.StackTrace);
                Console.ResetColor();
            }
            finally
            {
                if (connection != null)
                    connection.Disconnect();
            }
        }
    }
}