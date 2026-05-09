/** @type {import('next').NextConfig} */
const nextConfig = {
  //output: "standalone",
  images: {
    remotePatterns: [
      {
        protocol: "https",
        hostname: "images.unsplash.com",
      },
      {
        protocol: "https",
        hostname: "argillaceous-gwenn-overindulgent.ngrok-free.dev",
        pathname: "/uploads/**",
      },
    ],
  },
};

export default nextConfig;
