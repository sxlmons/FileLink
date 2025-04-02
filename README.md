# FileLink

A secure cloud storage and file management solution with a client-server architecture, allowing users to securely store, share, and manage their files from anywhere.


![File Link Wireframe](https://github.com/user-attachments/assets/41415f7f-f8fb-4389-ba3c-e1e95c05e3b0)


## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Architecture](#architecture)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Server Setup](#server-setup)
  - [Client Setup](#client-setup)
- [Usage](#usage)
- [Project Structure](#project-structure)
- [Protocol Specification](#protocol-specification)
- [Authentication](#authentication)
- [File Operations](#file-operations)
- [Directory Management](#directory-management)
- [Contributing](#contributing)
- [License](#license)

## Overview

FileLink is a client-server application that provides a cloud storage service. The server component manages user authentication, file storage, and directory management, while the client application provides a user-friendly interface for accessing these services from any device.

## Features

### User Management
- User registration and authentication
- Secure password storage using salt and hash
- Session management

### File Management
- Upload files to the cloud
- Download files from the cloud
- Delete files
- Display file metadata (size, type, creation date)

### Directory Management
- Create, rename, and delete directories
- Navigate directory structure
- Move files between directories
- Display directory contents

### User Interface
- Modern, intuitive UI with a responsive design
- File upload queue with progress tracking
- Directory navigation with breadcrumb support
- Search functionality

## Architecture

FileLink uses a client-server architecture with a custom TCP-based protocol for communication.

### Server Architecture

The server is built using .NET 9.0 and implements the following design patterns:

- **Command Pattern**: For handling different types of requests (login, file upload, directory navigation, etc.)
- **Repository Pattern**: For data storage abstraction
- **Factory Pattern**: For creating various objects like commands and session states
- **State Pattern**: For managing client session states (authentication, file transfer, etc.)

### Client Architecture

The client is built using .NET MAUI for cross-platform support and implements:

- **MVVM Pattern**: For separating UI logic from business logic
- **Service Layer**: For abstracting network communication and business operations
- **Repository Pattern**: For data access

## Getting Started

### Prerequisites

- .NET 9.0 SDK or later
- .NET MAUI workload (for client development)
- Visual Studio 2022 or later (recommended)

### Server Setup

1. Clone the repository
2. Navigate to the FileLink.Server directory
3. Build the server:
```
dotnet build
```
4. Run the server:
```
dotnet run
```

By default, the server listens on port 9000. You can change this by providing a port number as a command-line argument:
```
dotnet run -- 8080
```

### Client Setup

1. Navigate to the FileLink.Client directory
2. Build the client:
```
dotnet build
```
3. Run the client on your preferred platform:
```
dotnet run
```

## Usage

### Authentication

1. Launch the FileLink client
2. Create a new account or log in with your existing credentials
3. Configure server settings if needed (default: localhost:9000)

### Uploading Files

1. Click the "Upload" button in the sidebar
2. Select the files you want to upload
3. Click "Send Files" to start the upload process
4. Monitor upload progress

### Navigating Directories

1. Use the main content area to browse directories
2. Click on a directory to open it
3. Use the "BACK" button to navigate to the parent directory

### Managing Files and Directories

- Right-click on files or directories for context menu options
- Use the options to rename, delete, or move items

## Project Structure

### Server Components

- **Authentication**: User management and authentication services
- **Commands**: Handlers for different client request types
- **Protocol**: Packet definition and serialization
- **Disk**: Physical storage management
- **Network**: TCP server and client session management
- **Services**: Cross-cutting concerns like logging

### Client Components

- **Pages**: UI views (Login, Main)
- **Services**: Network, Authentication, File, and Directory services
- **Models**: Data models (User, FileItem, DirectoryItem)
- **Protocol**: Communication protocol implementation
- **FileOperations**: File upload/download operations
- **DirectoryNavigation**: Directory browsing functionality

## Protocol Specification

FileLink uses a custom binary protocol with the following packet structure:

1. Protocol Version (1 byte)
2. Command Code (4 bytes)
3. Packet ID (16 bytes)
4. User ID (variable length)
5. Timestamp (8 bytes)
6. Metadata (key-value pairs)
7. Payload (variable length)

Commands are categorized into:
- Authentication commands (100-199)
- File operations (200-299)
- Directory operations (240-251)
- Status responses (300-399)

## Authentication

The server implements a secure authentication system:
- Passwords are stored as salted hashes using PBKDF2 with SHA-256
- Session tokens for authenticated sessions
- Protection against brute force attacks with login attempt limits

## File Operations

Files are transferred in chunks to support large files and resume capabilities:
1. Initialize upload/download
2. Transfer chunks
3. Complete transfer

## Directory Management

Directories are organized in a hierarchical structure:
- Root directory for each user
- Nested directories supported
- Recursive operations (delete)
- Moving files between directories

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the LICENSE file for details.
