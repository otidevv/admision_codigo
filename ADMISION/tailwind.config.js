/** @type {import('tailwindcss').Config} */
module.exports = {
    darkMode: 'class',
    content: [
        "./Pages/**/*.cshtml",
        "./Views/**/*.cshtml",
        "./wwwroot/**/*.html",
        "./wwwroot/**/*.js"
    ],
    theme: {
        extend: {
            fontFamily: {
                sans:  ['Inter', '-apple-system', 'BlinkMacSystemFont', '"Segoe UI"', 'Roboto', '"Helvetica Neue"', 'Arial', 'sans-serif'],
                serif: ['Inter', '-apple-system', 'BlinkMacSystemFont', '"Segoe UI"', 'Roboto', '"Helvetica Neue"', 'Arial', 'sans-serif'],
                mono:  ['Inter', '-apple-system', 'BlinkMacSystemFont', '"Segoe UI"', 'Roboto', '"Helvetica Neue"', 'Arial', 'sans-serif'],
            },
            colors: {
                primary: {
                    DEFAULT: '#f54477',
                    50:  '#fef2f5',
                    100: '#fde6ec',
                    200: '#fbc0d0',
                    300: '#f999b4',
                    400: '#f76e98',
                    500: '#f54477',
                    600: '#f31a5b',
                    700: '#d10e49',
                    800: '#a00b38',
                    900: '#6e0827',
                },
                secondary: {
                    DEFAULT: '#716aca',
                    50:  '#f3f2fc',
                    100: '#e7e5f9',
                    200: '#c7c2f0',
                    300: '#a79fe7',
                    400: '#8f85d8',
                    500: '#716aca',
                    600: '#5a52b8',
                    700: '#4a4399',
                    800: '#3a347a',
                    900: '#2a255b',
                    1000: '#0f172a'
                },
                // Neutral cool scale, used for body / borders / typography in light & dark.
                ink: {
                    50:  '#f7f8fa',
                    100: '#eef0f4',
                    200: '#dce0e8',
                    300: '#bcc2cf',
                    400: '#8b93a5',
                    500: '#5c6478',
                    600: '#3f4658',
                    700: '#2a3041',
                    800: '#1a1f2e',
                    900: '#0e1220',
                    950: '#070912',
                },
                // Soft accents for category chips, mesh, decorative blobs.
                accent: {
                    peach:  '#ffb38a',
                    mint:   '#7be3c7',
                    violet: '#c8a3ff',
                    coral:  '#ff8a8a',
                },
            },
            boxShadow: {
                'glow': '0 30px 80px -20px rgba(245,68,119,0.45)',
                'glow-secondary': '0 30px 80px -20px rgba(113,106,202,0.45)',
                'card': '0 1px 0 rgba(255,255,255,0.04) inset, 0 8px 30px -8px rgba(0,0,0,0.18)',
                'soft': '0 0 0 1px rgba(15,18,32,0.06), 0 1px 2px rgba(15,18,32,0.04)',
            },
            transitionTimingFunction: {
                'out-expo': 'cubic-bezier(.2,.7,.2,1)',
            },
        },
    },
    plugins: [],
}
