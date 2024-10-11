import React, { useEffect, useState, useRef } from "react";
import { Box, Drawer, useMediaQuery, IconButton } from "@mui/material";
import ChatRoomList from "./ChatRoomList";
import ChatArea from "./ChatArea";
import MessageInput from "./MessageInput";
import * as signalR from "@microsoft/signalr";
import MenuIcon from "@mui/icons-material/Menu";
import DOMPurify from "dompurify";
import CryptoJS from "crypto-js"; // Importera CryptoJS

const drawerWidth = 240;

// AES-nyckel (32 tecken för AES-256). Ska matcha serverns nyckel.
const AES_KEY = "7CboWDwyMfsUsBXgi0fNa2UBt38Z4uM6"; // **OBS:** För produktion, hantera nyckeln säkert
const keyHex = CryptoJS.enc.Utf8.parse(AES_KEY);

// Funktion för att kryptera meddelanden
const encryptMessage = (message) => {
  const iv = CryptoJS.lib.WordArray.random(16); // Skapar en slumpmässig IV
  const encrypted = CryptoJS.AES.encrypt(message, keyHex, {
    iv: iv,
    mode: CryptoJS.mode.CBC,
    padding: CryptoJS.pad.Pkcs7,
  });
  const encryptedMessage = iv.concat(encrypted.ciphertext); // Kombinera IV + ciphertext
  return CryptoJS.enc.Base64.stringify(encryptedMessage); // Konvertera till Base64
};

// Funktion för att dekryptera meddelanden
const decryptMessage = (encryptedMessage) => {
  try {
    const encryptedBytes = CryptoJS.enc.Base64.parse(encryptedMessage);
    const iv = CryptoJS.lib.WordArray.create(
      encryptedBytes.words.slice(0, 4),
      16
    ); // IV är de första 16 byten (4 Word)
    const cipherText = CryptoJS.lib.WordArray.create(
      encryptedBytes.words.slice(4),
      encryptedBytes.sigBytes - 16
    );
    const decrypted = CryptoJS.AES.decrypt({ ciphertext: cipherText }, keyHex, {
      iv: iv,
      mode: CryptoJS.mode.CBC,
      padding: CryptoJS.pad.Pkcs7,
    });
    return decrypted.toString(CryptoJS.enc.Utf8); // Konverterar tillbaka till klartext
  } catch (error) {
    console.error("Dekrypteringsfel:", error);
    return "";
  }
};

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
      newConnection.on(
        "ReceiveMessage",
        (user, encryptedMessage, timestamp) => {
          if (!isMounted) return;

          // Dekryptera meddelandet
          const decryptedMessage = decryptMessage(encryptedMessage);

          // Om dekrypteringen misslyckas, använd det ursprungliga meddelandet
          const displayMessage =
            decryptedMessage !== "" ? decryptedMessage : encryptedMessage;

          // Saniterar användarnamn och meddelande
          const sanitizedUser = DOMPurify.sanitize(user);
          const sanitizedMessage = DOMPurify.sanitize(displayMessage);

          console.log("ReceiveMessage:", {
            user: sanitizedUser,
            message: sanitizedMessage,
            timestamp,
          });

          setMessages((prevMessages) => ({
            ...prevMessages,
            [selectedRoom]: [
              ...(prevMessages[selectedRoom] || []),
              {
                user: sanitizedUser,
                message: sanitizedMessage,
                timestamp: new Date(timestamp),
              },
            ],
          }));
        }
      );

      // Receive message history
      newConnection.on("ReceiveMessageHistory", (messageHistory) => {
        if (!isMounted) return;
        console.log("ReceiveMessageHistory:", messageHistory);

        const decryptedHistory = messageHistory.map((msg) => {
          const decryptedContent = decryptMessage(msg.Content);
          const displayMessage =
            decryptedContent !== "" ? decryptedContent : msg.Content;
          return {
            user: DOMPurify.sanitize(msg.Username),
            message: DOMPurify.sanitize(displayMessage),
            timestamp: new Date(msg.Timestamp),
          };
        });

        setMessages((prevMessages) => ({
          ...prevMessages,
          [selectedRoom]: decryptedHistory,
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
  }, [selectedRoom, token]); // Lägg till 'token' i beroende-listan

  const handleSendMessage = async (msg) => {
    const sanitizedMessage = DOMPurify.sanitize(msg);
    const encryptedMessage = encryptMessage(sanitizedMessage); // Kryptera meddelandet

    if (connectionRef.current && selectedRoom) {
      try {
        await connectionRef.current.invoke(
          "SendMessage",
          selectedRoom,
          encryptedMessage // Skicka det krypterade meddelandet
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
