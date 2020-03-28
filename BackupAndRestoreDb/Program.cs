using System;
using System.IO;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace BackupAndRestoreDb
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                var connectionString = ConfigurationManager.ConnectionStrings["Database"].ConnectionString;

                var backupDirectory = ConfigurationManager.AppSettings["BackupDirectory"];
                var backupFileName = $"Northwind_{ DateTime.Now:yyyy-MM-dd-HH-mm-ss}.bak";
                var backupFilePath = Path.Combine(backupDirectory, backupFileName);

                await BackupDB(backupFilePath, connectionString);
                await RestoreDB(backupFilePath, connectionString);

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

        private static async Task<bool> BackupDB(string backupFilePath, string connectionString)
        {
            var tcs = new TaskCompletionSource<bool>();

            try
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

                backup.PercentCompleteNotification = 10;

                backup.Complete += (s, e) =>
                {
                    // Inform the user that the backup has been completed 
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(e.Error.Message);
                    Console.ResetColor();

                    connection.Disconnect();

                    tcs.SetResult(true);
                };

                backup.PercentComplete += (s, e) =>
                {
                    // Inform the user percent complete
                    Console.WriteLine($"Percent Complete: {e.Percent}");
                };

                backup.Information += (s, e) =>
                {
                    if (e.Error.Message.Contains("Cannot open backup device") || e.Error.Message.Contains("terminating abnormally"))
                    {
                        Console.WriteLine($"Backup Error: {e.Error.Message}");
                        tcs.TrySetException(new Exception(e.Error.Message));
                    }
                    else
                    {
                        Console.WriteLine($"Backup Information: {e.Error.Message}");
                    }
                };

                // Run SqlBackup to perform the full database backup on the instance of SQL Server
                backup.SqlBackupAsync(sqlServer);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }

            return await tcs.Task;
        }

        private static async Task<bool> RestoreDB(string backupFilePath, string connectionString)
        {
            var tcs = new TaskCompletionSource<bool>();

            try
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

                restore.PercentCompleteNotification = 10;

                // Setup a new connection to the data server
                ServerConnection connection = new ServerConnection(new SqlConnection(connectionString));
                Server sqlServer = new Server(connection);

                restore.Complete += (s, e) =>
                {
                    // Inform the user that the restore has been completed
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(e.Error.Message);
                    Console.ResetColor();

                    connection.Disconnect();

                    tcs.SetResult(true);
                };

                restore.PercentComplete += (s, e) =>
                {
                    // Inform the user percent complete
                    Console.WriteLine($"Percent Complete: {e.Percent}");
                };

                restore.Information += (s, e) =>
                {
                    Console.WriteLine($"Restore Information: {e.Error.Message}");
                };

                // Restore the full database backup with recovery         
                restore.SqlRestoreAsync(sqlServer);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }

            return await tcs.Task;
        }
    }
}