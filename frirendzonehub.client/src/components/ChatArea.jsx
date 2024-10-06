import React, { useRef, useEffect } from "react";
import { Box, Typography } from "@mui/material";

const ChatArea = ({ messages }) => {
  const scrollRef = useRef();

  useEffect(() => {
    scrollRef.current?.scrollIntoView({ behavior: "smooth" });
    console.log(messages);
  }, [messages]);

  return (
    <Box className="chatarea" flexGrow={1} p={2} overflow="auto">
      {messages.map((msg, index) => (
        <Box key={index} mb={2}>
          <Typography variant="subtitle2">
            {msg.user}{" "}
            <span style={{ fontSize: "0.8em", color: "gray" }}>
              {new Date(msg.timestamp).toLocaleTimeString()}
            </span>
          </Typography>

          <Typography variant="body1">{msg.message}</Typography>
        </Box>
      ))}
      <div ref={scrollRef} />
    </Box>
  );
};

export default ChatArea;
