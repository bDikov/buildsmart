const https = require('https');

const PROJECT_API_KEY = 'phc_yUmkAiv9JtSfVG72BF4WKbRfmLFsLVkzrM588xcW329C';
const POSTHOG_HOST = 'us.i.posthog.com';

function capture(event, distinctId, properties = {}) {
    return new Promise((resolve, reject) => {
        const payload = JSON.stringify({
            api_key: PROJECT_API_KEY,
            event: event,
            properties: {
                distinct_id: distinctId,
                $lib: 'node-test-script',
                ...properties
            }
        });

        const options = {
            hostname: POSTHOG_HOST,
            port: 443,
            path: '/capture/',
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Content-Length': Buffer.byteLength(payload)
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
        req.write(payload);
        req.end();
    });
}

// Generate some random distinct IDs to represent different users
function generateUser() {
    return 'user_' + Math.random().toString(36).substring(2, 11);
}

async function run() {
    console.log('Sending mock campaign data to PostHog...');

    // Scenario A: 5 Users from Facebook "summer_promo" Campaign
    // 5 started wizard -> 3 submitted estimate
    console.log('\n📱 Simulating Facebook Summer Promo Campaign...');
    for (let i = 0; i < 5; i++) {
        const userId = generateUser();
        const props = {
            utm_source: 'facebook',
            utm_medium: 'cpc',
            utm_campaign: 'summer_promo',
            $current_url: 'https://buildsmart.bg/?utm_source=facebook&utm_campaign=summer_promo'
        };

        // Step 1: Start Wizard
        await capture('wizard_started', userId, props);
        console.log(`- User ${userId} started wizard`);

        // Step 2: 3 out of 5 submit
        if (i < 3) {
            await capture('ai_estimate_generated', userId, props);
            console.log(`- User ${userId} submitted estimate!`);
        }
    }

    // Scenario B: 3 Users from Google Search "renovations" Campaign
    // 3 started wizard -> 1 submitted estimate
    console.log('\n🔍 Simulating Google Search Campaign...');
    for (let i = 0; i < 3; i++) {
        const userId = generateUser();
        const props = {
            utm_source: 'google',
            utm_medium: 'organic',
            utm_campaign: 'renovations_search',
            $current_url: 'https://buildsmart.bg/?utm_source=google&utm_campaign=renovations_search'
        };

        await capture('wizard_started', userId, props);
        console.log(`- User ${userId} started wizard`);

        if (i < 1) {
            await capture('ai_estimate_generated', userId, props);
            console.log(`- User ${userId} submitted estimate!`);
        }
    }

    // Scenario C: 2 Organic users (No UTM params)
    // 2 started -> 1 submitted
    console.log('\n🍃 Simulating Organic/Direct Traffic...');
    for (let i = 0; i < 2; i++) {
        const userId = generateUser();
        const props = {
            $current_url: 'https://buildsmart.bg/'
        };

        await capture('wizard_started', userId, props);
        console.log(`- User ${userId} started wizard`);

        if (i < 1) {
            await capture('ai_estimate_generated', userId, props);
            console.log(`- User ${userId} submitted estimate!`);
        }
    }

    console.log('\n🎉 Finished sending mock events! Check your PostHog Dashboard now.');
}

run();
