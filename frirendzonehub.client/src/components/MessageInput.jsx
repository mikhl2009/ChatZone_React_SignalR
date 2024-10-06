import React, { useState } from "react";
import { Box, TextField, Button } from "@mui/material";

const MessageInput = ({ onSend }) => {
  const [newMessage, setNewMessage] = useState("");

  const handleSend = () => {
    onSend(newMessage);
    setNewMessage("");
  };

  return (
    <Box display="flex" p={2}>
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
