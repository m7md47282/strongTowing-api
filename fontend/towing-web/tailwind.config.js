/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./src/**/*.{html,ts}'],
  theme: {
    extend: {
      colors: {
        primary: {
          DEFAULT: 'var(--primary-blue)',
          blue: 'var(--primary-blue)',
          blueAlt: 'var(--primary-blue-alt)',
          dark: 'var(--dark-blue)',
          light: 'var(--light-blue)',
          orange: 'var(--primary-orange)',
          orangeHover: 'var(--orange-hover)',
        },
        gray: {
          50: 'var(--gray-50)',
          100: 'var(--gray-100)',
          200: 'var(--gray-200)',
          300: 'var(--gray-300)',
          400: 'var(--gray-400)',
          500: 'var(--gray-500)',
          600: 'var(--gray-600)',
          700: 'var(--gray-700)',
          800: 'var(--gray-800)',
          900: 'var(--gray-900)',
        },
        blue: {
          100: 'var(--blue-100)',
          400: 'var(--blue-400)',
          500: 'var(--blue-500)',
          600: 'var(--blue-600)',
        },
        orange: {
          500: 'var(--orange-500)',
          600: 'var(--orange-600)',
        },
        green: {
          500: 'var(--green-500)',
          600: 'var(--green-600)',
        },
        pink: {
          500: 'var(--pink-500)',
        },
        red: {
          500: 'var(--red-500)',
          600: 'var(--red-600)',
        },
      },
    },
  },
  plugins: [],
};
