import React, { useState, useEffect } from "react";
import { Box, Drawer, List, ListItemText, ListItemButton } from "@mui/material";
import ChatRoomList from "./ChatRoomList";
import ChatArea from "./ChatArea";
import MessageInput from "./MessageInput";
import * as signalR from "@microsoft/signalr";

const Layout = () => {
  const [selectedRoom, setSelectedRoom] = useState(null);
  const [messages, setMessages] = useState({});
  const [connection, setConnection] = useState(null);
  const token = localStorage.getItem("authToken");

  if (!token) {
    console.error("No authentication token found");
    return;
  }

  useEffect(() => {
    const connect = async () => {
      if (!selectedRoom) return; // Gå inte vidare om inget rum är valt

      const newConnection = new signalR.HubConnectionBuilder()
        .withUrl("https://localhost:7167/chathub", {
          accessTokenFactory: () => token,
        })
        .configureLogging(signalR.LogLevel.Information)
        .withServerTimeout(60000)
        .build();

      newConnection.on("ReceiveMessage", (user, message, timestamp) => {
        console.log("Message received from server:", user, message, timestamp);
        setMessages((prevMessages) => ({
          ...prevMessages,
          [selectedRoom]: [
            ...(prevMessages[selectedRoom] || []),
            { user, message, timestamp: new Date() },
          ],
        }));
      });

      try {
        await newConnection.start();
        console.log(`Connected to room: ${selectedRoom}`);
        await newConnection.invoke("JoinRoom", selectedRoom); // Join the selected room
      } catch (error) {
        console.log("Connection failed:", error);
      }

      setConnection(newConnection);

      return () => {
        newConnection.off("ReceiveMessage");
        newConnection.stop();
        console.log(`Disconnected from room: ${selectedRoom}`);
      };
    };

    connect();
  }, [selectedRoom]); // Effekt körs när `selectedRoom` ändras

  const handleSendMessage = async (msg) => {
    if (connection && selectedRoom) {
      try {
        console.log("Sending message to room:", selectedRoom);
        await connection.invoke("SendMessage", selectedRoom, msg);
      } catch (error) {
        console.log("Error sending message:", error);
      }
    } else {
      console.log("No room selected or no connection available.");
    }
  };

  return (
    <Box display="flex" height="100vh">
      <Drawer variant="permanent" anchor="left">
        <ChatRoomList
          selectedRoom={selectedRoom}
          setSelectedRoom={setSelectedRoom}
        />
      </Drawer>
      <Box flexGrow={1} display="flex" flexDirection="column">
        <ChatArea messages={messages[selectedRoom] || []} />
        <MessageInput onSend={handleSendMessage} />
      </Box>
    </Box>
  );
};

export default Layout;
