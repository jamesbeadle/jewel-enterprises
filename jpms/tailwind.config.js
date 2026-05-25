/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './**/*.razor',
    './wwwroot/index.html'
  ],
  theme: {
    extend: {
      fontFamily: {
        sans: [
          '-apple-system',
          'BlinkMacSystemFont',
          'Segoe UI',
          'system-ui',
          'sans-serif'
        ]
      }
    }
  },
  plugins: []
};
