const https = require('https');

const PERSONAL_API_KEY = process.argv[2];
const PROJECT_ID = '497328';
const POSTHOG_HOST = 'us.posthog.com';

if (!PERSONAL_API_KEY) {
    console.error('Error: Please provide your PostHog Personal API Key as an argument.');
    process.exit(1);
}

function getDashboards() {
    return new Promise((resolve, reject) => {
        const options = {
            hostname: POSTHOG_HOST,
            port: 443,
            path: `/api/projects/${PROJECT_ID}/dashboards/`,
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${PERSONAL_API_KEY}`,
                'Content-Type': 'application/json'
            }
        };

        const req = https.request(options, (res) => {
            let data = '';
            res.on('data', (chunk) => data += chunk);
            res.on('end', () => {
                if (res.statusCode >= 200 && res.statusCode < 300) {
                    resolve(JSON.parse(data));
                } else {
                    reject(new Error(`Failed with status ${res.statusCode}: ${data}`));
                }
            });
        });

        req.on('error', (e) => reject(e));
        req.end();
    });
}

async function run() {
    try {
        console.log('Fetching dashboards...');
        const res = await getDashboards();
        // Print the first dashboard that has tiles, if any
        const firstWithTiles = res.results.find(d => d.tiles && d.tiles.length > 0) || res.results[0];
        console.log('\n--- Dashboard JSON ---');
        console.log(JSON.stringify(firstWithTiles, null, 2));
    } catch (e) {
        console.error('Error:', e.message);
    }
}

run();
