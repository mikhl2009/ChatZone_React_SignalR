import React, { useEffect, useState, useRef } from "react";
import { Box, Drawer, useMediaQuery, IconButton } from "@mui/material";
import ChatRoomList from "./ChatRoomList";
import ChatArea from "./ChatArea";
import MessageInput from "./MessageInput";
import * as signalR from "@microsoft/signalr";
import MenuIcon from "@mui/icons-material/Menu";
import DOMPurify from "dompurify";

const drawerWidth = 240;

const Layout = () => {
  const [selectedRoom, setSelectedRoom] = useState(null);
  const [messages, setMessages] = useState({});
  const connectionRef = useRef(null);
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
    let isMounted = true;
    let newConnection;

    const connect = async () => {
      if (!selectedRoom) return;

      // Stop previous connection if it exists
      if (connectionRef.current) {
        try {
          await connectionRef.current.stop();
          console.log("Disconnected from previous room.");
        } catch (err) {
          console.error("Error stopping previous connection:", err);
        }
      }

      newConnection = new signalR.HubConnectionBuilder()
        .withUrl("https://localhost:7167/chathub", {
          accessTokenFactory: () => token,
        })
        .configureLogging(signalR.LogLevel.Information)
        .withAutomaticReconnect()
        .build();

      // Receive real-time messages
      newConnection.on("ReceiveMessage", (user, message, timestamp) => {
        if (!isMounted) return;
        console.log("ReceiveMessage:", { user, message, timestamp });
        setMessages((prevMessages) => ({
          ...prevMessages,
          [selectedRoom]: [
            ...(prevMessages[selectedRoom] || []),
            { user, message, timestamp: new Date(timestamp) },
          ],
        }));
      });

      // Receive message history
      newConnection.on("ReceiveMessageHistory", (messageHistory) => {
        if (!isMounted) return;
        console.log("ReceiveMessageHistory:", messageHistory);
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
        connectionRef.current = newConnection;
      } catch (error) {
        console.log("Connection failed:", error);
      }
    };

    connect();

    return () => {
      isMounted = false;
      if (newConnection) {
        newConnection.off("ReceiveMessage");
        newConnection.off("ReceiveMessageHistory");
        newConnection.stop();
        console.log(`Disconnected from room: ${selectedRoom}`);
        connectionRef.current = null;
      }
    };
  }, [selectedRoom]);

  const handleSendMessage = async (msg) => {
    const sanitizedMessage = DOMPurify.sanitize(msg);
    if (connectionRef.current && selectedRoom) {
      try {
        await connectionRef.current.invoke(
          "SendMessage",
          selectedRoom,
          sanitizedMessage
        );
      } catch (error) {
        console.log("Error sending message:", error);
      }
    } else {
      console.log("No room selected or no connection available.");
    }
  };

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
        <ChatRoomList
          selectedRoom={selectedRoom}
          setSelectedRoom={setSelectedRoom}
        />
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
        {selectedRoom && <MessageInput onSend={handleSendMessage} />}
      </Box>
    </Box>
  );
};

export default Layout;
