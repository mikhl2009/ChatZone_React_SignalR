import React, { useState, useEffect } from "react";
import {
  Box,
  Drawer,
  List,
  ListItemButton,
  ListItemText,
  useMediaQuery,
  IconButton,
} from "@mui/material";
import ChatRoomList from "./ChatRoomList";
import ChatArea from "./ChatArea";
import MessageInput from "./MessageInput";
import * as signalR from "@microsoft/signalr";
import MenuIcon from "@mui/icons-material/Menu"; // for hamburger menu icon

const drawerWidth = 240;

const Layout = () => {
  const [selectedRoom, setSelectedRoom] = useState(null);
  const [messages, setMessages] = useState({});
  const [connection, setConnection] = useState(null);
  const [mobileOpen, setMobileOpen] = useState(false);
  const token = localStorage.getItem("authToken");

  const isMobile = useMediaQuery("(max-width:600px)");

  const handleDrawerToggle = () => {
    setMobileOpen(!mobileOpen);
  };

  if (!token) {
    console.error("No authentication token found");
    return null;
  }

  useEffect(() => {
    const connect = async () => {
      if (!selectedRoom) return;

      if (connection) {
        await connection.stop();
        console.log("Disconnected from previous room.");
      }
      const newConnection = new signalR.HubConnectionBuilder()
        .withUrl("https://localhost:7167/chathub", {
          accessTokenFactory: () => token,
        })
        .configureLogging(signalR.LogLevel.Information)
        .withServerTimeout(60000)
        .build();

      // Receive real-time messages
      newConnection.on("ReceiveMessage", (user, message, timestamp) => {
        setMessages((prevMessages) => ({
          ...prevMessages,
          [selectedRoom]: [
            ...(prevMessages[selectedRoom] || []),
            { user, message, timestamp: new Date() },
          ],
        }));
      });

      // Receive message history
      newConnection.on("ReceiveMessageHistory", (messageHistory) => {
        setMessages((prevMessages) => ({
          ...prevMessages,
          [selectedRoom]: messageHistory.map((msg) => ({
            user: msg.Username,
            message: msg.Content,
            timestamp: new Date(msg.Timestamp),
          })),
        }));
      });

      try {
        await newConnection.start();
        console.log(`Connected to room: ${selectedRoom}`);
        await newConnection.invoke("JoinRoom", selectedRoom);
      } catch (error) {
        console.log("Connection failed:", error);
      }

      setConnection(newConnection);

      return () => {
        newConnection.off("ReceiveMessage");
        newConnection.off("ReceiveMessageHistory");
        newConnection.stop();
        console.log(`Disconnected from room: ${selectedRoom}`);
      };
    };

    connect();
  }, [selectedRoom, token]);

  const handleSendMessage = async (msg) => {
    if (connection && selectedRoom) {
      try {
        await connection.invoke("SendMessage", selectedRoom, msg);
      } catch (error) {
        console.log("Error sending message:", error);
      }
    } else {
      console.log("No room selected or no connection available.");
    }
  };

  const drawer = (
    <List>
      <ListItemButton
        selected={selectedRoom === "General"}
        onClick={() => setSelectedRoom("General")}
      >
        <ListItemText primary="General" />
      </ListItemButton>
      <ListItemButton
        selected={selectedRoom === "Tech Talk"}
        onClick={() => setSelectedRoom("Tech Talk")}
      >
        <ListItemText primary="Tech Talk" />
      </ListItemButton>
      <ListItemButton
        selected={selectedRoom === "Random"}
        onClick={() => setSelectedRoom("Random")}
      >
        <ListItemText primary="Random" />
      </ListItemButton>
    </List>
  );

  return (
    <Box display="flex" height="100vh">
      {isMobile && (
        <IconButton
          color="inherit"
          aria-label="open drawer"
          edge="start"
          onClick={handleDrawerToggle}
          sx={{ position: "absolute", top: 0, left: 0, zIndex: 1 }}
        >
          <MenuIcon />
        </IconButton>
      )}
      <Drawer
        variant={isMobile ? "temporary" : "permanent"}
        open={mobileOpen}
        onClose={handleDrawerToggle}
        sx={{
          width: drawerWidth,
          flexShrink: 0,
          [`& .MuiDrawer-paper`]: {
            width: drawerWidth,
            boxSizing: "border-box",
          },
        }}
      >
        {drawer}
      </Drawer>
      <Box
        component="main"
        sx={{
          flexGrow: 1,
          display: "flex",
          flexDirection: "column",
          height: "100vh",
          overflow: "hidden",
        }}
      >
        <ChatArea messages={messages[selectedRoom] || []} />
        <MessageInput onSend={handleSendMessage} />
      </Box>
    </Box>
  );
};

export default Layout;
