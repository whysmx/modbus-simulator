/** @type {import('next').NextConfig} */
const nextConfig = {
  eslint: {
    ignoreDuringBuilds: true,
  },
  typescript: {
    ignoreBuildErrors: true,
  },
  images: {
    unoptimized: true,
  },
  async rewrites() {
    return [
      {
        source: "/api/:path*",
        destination: "http://localhost:5000/api/:path*",
      },
    ]
  },
  // Add custom webpack config to handle SSL issues  
  webpack: (config, { buildId, dev, isServer, defaultLoaders, webpack }) => {
    if (isServer && dev) {
      // For development server, ignore SSL certificate errors
      process.env["NODE_TLS_REJECT_UNAUTHORIZED"] = "0";
    }
    return config;
  },
}

export default nextConfig
