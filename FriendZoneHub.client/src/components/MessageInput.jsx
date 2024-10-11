import React, { useState } from "react";
import { Box, TextField, Button, IconButton, Popover } from "@mui/material";
import EmojiEmotionsIcon from "@mui/icons-material/EmojiEmotions";
import EmojiPicker from "emoji-picker-react"; // Updated import

const MessageInput = ({ onSend }) => {
  const [newMessage, setNewMessage] = useState("");
  const [anchorEl, setAnchorEl] = useState(null);

  const handleSend = () => {
    if (newMessage.trim()) {
      onSend(newMessage);
      setNewMessage("");
    }
  };

  const handleEmojiClick = (emojiObject) => {
    setNewMessage((prev) => prev + emojiObject.emoji);
  };

  const handleClick = (event) => {
    setAnchorEl(event.currentTarget);
  };

  const handleClose = () => {
    setAnchorEl(null);
  };

  const open = Boolean(anchorEl);
  const id = open ? "simple-popover" : undefined;

  return (
    <Box display="flex" p={2} bgcolor="#1e1e1e">
      <IconButton onClick={handleClick}>
        <EmojiEmotionsIcon style={{ color: "#ffffff" }} />
      </IconButton>
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

      {/* Emoji Picker Popover */}
      <Popover
        id={id}
        open={open}
        anchorEl={anchorEl}
        onClose={handleClose}
        anchorOrigin={{
          vertical: "bottom",
          horizontal: "left",
        }}
        transformOrigin={{
          vertical: "top",
          horizontal: "left",
        }}
      >
        <EmojiPicker onEmojiClick={handleEmojiClick} />
      </Popover>
    </Box>
  );
};

export default MessageInput;
