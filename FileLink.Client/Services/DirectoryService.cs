using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using FileLink.Client.Models;
using FileLink.Client.Protocol;
using FileLink.Client.Services;

namespace FileLink.Client.Services
{
    // Provides directory management services for the cloud file client
    public class DirectoryService
    {
        private readonly NetworkService _networkService;
        private readonly PacketFactory _packetFactory = new PacketFactory();
        
        // Initializes a new instance of the DirectoryService class
        public DirectoryService(NetworkService networkService)
        {
            _networkService = networkService ?? throw new ArgumentNullException(nameof(networkService));
        }
        
        // Gets the contents of a directory
        public async Task<(List<FileItem> Files, List<DirectoryItem> Directories)> GetDirectoryContentsAsync(
            string? directoryId, string userId)
        {
            var files = new List<FileItem>();
            var directories = new List<DirectoryItem>();

            try
            {
                // Create the directory contents request packet
                var packet = new Packet(Commands.CommandCode.DIRECTORY_CONTENTS_REQUEST)
                {
                    UserId = userId
                };

                // Set the directory ID in metadata
                if (!string.IsNullOrEmpty(directoryId))
                {
                    packet.Metadata["DirectoryId"] = directoryId;
                }
                else
                {
                    packet.Metadata["DirectoryId"] = "root";
                }

                // Send the packet and get the response
                var response = await _networkService.SendAndReceiveAsync(packet);

                if (response == null || response.CommandCode == Commands.CommandCode.ERROR)
                {
                    return (files, directories);
                }

                if (response.Payload == null || response.Payload.Length == 0)
                {
                    return (files, directories);
                }

                // Deserialize the response
                var directoryContents = JsonSerializer.Deserialize<DirectoryContentsResponse>(response.Payload);

                if (directoryContents == null)
                {
                    return (files, directories);
                }

                // Convert to models
                files = directoryContents.Files ?? new List<FileItem>();
                directories = directoryContents.Directories ?? new List<DirectoryItem>();

                return (files, directories);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting directory contents: {ex.Message}");
                return (files, directories);
            }
        }
        
        // Creates a new directory
        public async Task<DirectoryItem?> CreateDirectoryAsync(string directoryName, string? parentDirectoryId, string userId)
        {
            try
            {
                // Create directory info for serialization
                var directoryInfo = new
                {
                    DirectoryName = directoryName,
                    ParentDirectoryId = parentDirectoryId
                };

                // Serialize to JSON
                var payload = JsonSerializer.SerializeToUtf8Bytes(directoryInfo);

                // Create the directory creation request packet
                var packet = new Packet(Commands.CommandCode.DIRECTORY_CREATE_REQUEST)
                {
                    UserId = userId,
                    Payload = payload
                };

                // Send the packet and get the response
                var response = await _networkService.SendAndReceiveAsync(packet);

                if (response == null || response.CommandCode == Commands.CommandCode.ERROR)
                {
                    return null;
                }

                if (response.Payload == null || response.Payload.Length == 0)
                {
                    return null;
                }

                // Deserialize the response
                var createDirectoryResponse = JsonSerializer.Deserialize<CreateDirectoryResponse>(response.Payload);

                if (createDirectoryResponse == null || !createDirectoryResponse.Success)
                {
                    return null;
                }

                // Create and return the new directory
                return new DirectoryItem
                {
                    Id = createDirectoryResponse.DirectoryId,
                    Name = createDirectoryResponse.DirectoryName,
                    ParentDirectoryId = parentDirectoryId,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating directory: {ex.Message}");
                return null;
            }
        }
        
        // Deletes a directory
        public async Task<bool> DeleteDirectoryAsync(string directoryId, bool recursive, string userId)
        {
            try
            {
                // Create the directory deletion request packet
                var packet = new Packet(Commands.CommandCode.DIRECTORY_DELETE_REQUEST)
                {
                    UserId = userId
                };

                packet.Metadata["DirectoryId"] = directoryId;
                packet.Metadata["Recursive"] = recursive.ToString();

                // Send the packet and get the response
                var response = await _networkService.SendAndReceiveAsync(packet);

                if (response == null || response.CommandCode == Commands.CommandCode.ERROR)
                {
                    return false;
                }

                if (response.Payload == null || response.Payload.Length == 0)
                {
                    return false;
                }

                // Deserialize the response
                var deleteDirectoryResponse = JsonSerializer.Deserialize<DeleteDirectoryResponse>(response.Payload);

                return deleteDirectoryResponse?.Success ?? false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting directory: {ex.Message}");
                return false;
            }
        }

        // Helper classes for deserializing responses
        private class DirectoryContentsResponse
        {
            public List<FileItem>? Files { get; set; }
            public List<DirectoryItem>? Directories { get; set; }
            public string? DirectoryId { get; set; }
        }

        private class CreateDirectoryResponse
        {
            public bool Success { get; set; }
            public string DirectoryId { get; set; } = "";
            public string DirectoryName { get; set; } = "";
            public string Message { get; set; } = "";
        }

        private class DeleteDirectoryResponse
        {
            public bool Success { get; set; }
            public string DirectoryId { get; set; } = "";
            public string Message { get; set; } = "";
        }
    }
    
    // Extension methods for DirectoryService
    public static class DirectoryServiceExtensions
    {
        // Get a specific directory by ID
        public static async Task<DirectoryItem> GetDirectoryByIdAsync(this DirectoryService service,
            string directoryId, string userId)
        {
            if (string.IsNullOrEmpty(directoryId) || string.IsNullOrEmpty(userId))
                return null;

            try
            {
                // Get all directories first
                var (_, directories) = await service.GetDirectoryContentsAsync(null, userId);

                // Find specific directory - this is inefficient but works until 
                // a proper GetDirectoryById endpoint is added to DirectoryService
                return directories.FirstOrDefault(d => d.Id == directoryId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting directory by ID: {ex.Message}");
                return null;
            }
        }
    }
}