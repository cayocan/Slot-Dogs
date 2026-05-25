import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Permite hospedar o build WebGL do Unity em /public/game
  // e embuti-lo via iframe sem restrições de COOP/COEP
  async headers() {
    return [
      {
        source: "/(.*)",
        headers: [
          { key: "Cross-Origin-Opener-Policy", value: "same-origin" },
          { key: "Cross-Origin-Embedder-Policy", value: "credentialless" },
        ],
      },
    ];
  },
};

export default nextConfig;
