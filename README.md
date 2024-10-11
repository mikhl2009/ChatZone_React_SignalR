# ChatZone Chat Application

Welcome to the ChatZone Application! This guide will walk you through the setup process to run the application locally. The application consists of a backend (.NET server) and a frontend (React). Please follow the steps below to ensure everything works smoothly.

## Prerequisites

- Node.js and npm or yarn.
- .NET 8 SDK (or later).
- Visual Studio 2022 or VS Code for .NET development.
- MySQL to set up the database.

## Setup Instructions

### 1. Clone the Repository

First, clone the repository from GitHub:

```sh
git clone https://github.com/mikhl2009/FriendZone_React_SignalR.git
cd FriendZoneChat
```

### 2. Backend Setup (ASP.NET Core)

#### Navigate to the Server Directory

```sh
cd FriendZoneHub.Server
```

#### Configure the Database

Update the connection string in `appsettings.json` to point to your MySQL for local testing.

Run the following command to apply database migrations:

```sh
dotnet ef database update
```

#### Run the Server

Start the backend server by running:

```sh
dotnet run
```

The server should be accessible at `https://localhost:7167`.

### 3. Frontend Setup (React)

#### Navigate to the Client Directory

```sh
cd FriendZoneHub.Client
```

#### Install Dependencies

Install the required npm packages by running:

```sh
npm install
```

#### Run the Client

Start the React development server:

```sh
npm start
```

The client should be accessible at `http://localhost:5173`.

### 4. Application Configuration

- **Authentication Token**: Make sure to store the JWT authentication token in `localStorage` to allow seamless integration between client and server.
- **HTTPS**: Since the backend uses HTTPS, ensure the frontend points to the correct secure endpoint (`https://localhost:7167`).

### 5. Testing the Application

- **Joining a Chat Room**: Once both the server and client are running, navigate to `http://localhost:5173` and select a chat room to join.
- **Sending Messages**: Messages are encrypted/decrypted on both server and client sides using AES-256. Make sure the messages appear correctly in the chat room after encryption and decryption.

### 6. Troubleshooting

- **Database Connection Issues**: Double-check the connection string in `appsettings.json`.
- **CORS Errors**: Ensure the backend server allows requests from `http://localhost:5173`.
- **Connection Refused**: Verify that both backend and frontend are running, and that the URLs and ports match as expected.

### 7. Stopping the Application

- **Backend**: Press `CTRL + C` in the terminal where the backend server is running.
- **Frontend**: Press `CTRL + C` in the terminal where the frontend server is running.

## Additional Notes

- **Production Setup**: For production, replace the self-signed certificate with a trusted SSL certificate, and securely manage the AES key used for message encryption.
- **Environment Variables**: Consider moving sensitive configurations (e.g., database connection strings and encryption keys) to environment variables.

## Contributions

We welcome contributions! Please fork the repository, make your changes, and create a pull request.

## License

This project is licensed under the MIT License.
