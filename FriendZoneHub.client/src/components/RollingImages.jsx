import React from "react";
import { Box } from "@mui/material";

const images = [
  "https://images.unsplash.com/photo-1516321318423-f06f85e504b3?ixlib=rb-1.2.1&auto=format&fit=crop&w=300&q=80",
  "https://images.unsplash.com/photo-1573164713988-8665fc963095?ixlib=rb-1.2.1&auto=format&fit=crop&w=300&q=80",
  "https://images.unsplash.com/photo-1543269865-cbf427effbad?ixlib=rb-1.2.1&auto=format&fit=crop&w=300&q=80",
  "https://images.unsplash.com/photo-1587825140708-dfaf72ae4b04?ixlib=rb-1.2.1&auto=format&fit=crop&w=300&q=80",
];

const RollingImages = () => {
  return (
    <Box sx={{ overflow: "hidden", mt: 4 }}>
      <Box sx={{ display: "flex", animation: "scroll 30s linear infinite" }}>
        {[...images, ...images].map((src, index) => (
          <img
            key={index}
            src={src}
            alt={`Chat preview ${index + 1}`}
            style={{
              width: "200px",
              height: "200px",
              objectFit: "cover",
              borderRadius: "8px",
              margin: "0 8px",
            }}
          />
        ))}
      </Box>
    </Box>
  );
};

export default RollingImages;
