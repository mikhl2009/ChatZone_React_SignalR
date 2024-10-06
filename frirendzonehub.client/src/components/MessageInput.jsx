import React, { useState } from "react";
import { Box, TextField, Button } from "@mui/material";

const MessageInput = ({ onSend }) => {
  const [newMessage, setNewMessage] = useState("");

  const handleSend = () => {
    if (newMessage.trim()) {
      onSend(newMessage);
      setNewMessage("");
    }
  };

  return (
    <Box display="flex" p={2} bgcolor="#1e1e1e">
      <TextField
        fullWidth
        variant="outlined"
        placeholder="Type your message..."
        value={newMessage}
        onChange={(e) => setNewMessage(e.target.value)}
        onKeyPress={(e) => {
          if (e.key === "Enter") {
            handleSend();
            e.preventDefault();
          }
        }}
        InputProps={{
          style: { backgroundColor: "#2c2c2c", color: "#ffffff" },
        }}
      />
      <Button
        variant="contained"
        color="primary"
        onClick={handleSend}
        sx={{ ml: 2 }}
      >
        Send
      </Button>
    </Box>
  );
};

export default MessageInput;
