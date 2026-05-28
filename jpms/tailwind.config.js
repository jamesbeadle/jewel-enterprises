/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './**/*.razor',
    './**/*.cs',
    './wwwroot/index.html'
  ],
  theme: {
    extend: {
      fontFamily: {
        sans: [
          'Poppins',
          '-apple-system',
          'BlinkMacSystemFont',
          'Segoe UI',
          'system-ui',
          'sans-serif'
        ]
      },
      colors: {
        canvas: '#0B0B0C',
        surface: {
          DEFAULT: '#161719',
          raised: '#1F2024',
          field: '#2A2D31'
        },
        line: {
          DEFAULT: '#232427',
          strong: '#34373B'
        },
        content: {
          DEFAULT: '#FFFFFF',
          muted: '#C4C8CE',
          subtle: '#8A9099',
          faint: '#5A5F68'
        },
        accent: {
          DEFAULT: '#57E08A',
          hover: '#4ECF7E',
          ink: '#0B0B0C'
        },
        positive: '#4ED07D',
        negative: '#FF4D4D',
        info: '#4691F6'
      }
    }
  },
  plugins: []
};
