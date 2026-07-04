const https = require('https');

const PERSONAL_API_KEY = process.argv[2];
const PROJECT_ID = '497328';
const POSTHOG_HOST = 'us.posthog.com';

if (!PERSONAL_API_KEY) {
    console.error('Error: Please provide your PostHog Personal API Key as an argument.');
    console.error('Usage: node scratch/create_dashboard.js phx_your_personal_key_here');
    process.exit(1);
}

function request(path, method, body) {
    return new Promise((resolve, reject) => {
        const payload = JSON.stringify(body);
        const options = {
            hostname: POSTHOG_HOST,
            port: 443,
            path: `/api/projects/${PROJECT_ID}${path}`,
            method: method,
            headers: {
                'Authorization': `Bearer ${PERSONAL_API_KEY}`,
                'Content-Type': 'application/json',
                'Content-Length': Buffer.byteLength(payload)
            }
        };

        const req = https.request(options, (res) => {
            let data = '';
            res.on('data', (chunk) => data += chunk);
            res.on('end', () => {
                if (res.statusCode >= 200 && res.statusCode < 300) {
                    try {
                        resolve(JSON.parse(data));
                    } catch (e) {
                        resolve(data);
                    }
                } else {
                    reject(new Error(`Request failed with status ${res.statusCode}: ${data}`));
                }
            });
        });

        req.on('error', (e) => reject(e));
        req.write(payload);
        req.end();
    });
}

async function run() {
    try {
        console.log('🚀 Creating "Estimate Funnel & Campaigns" dashboard...');
        const dashboard = await request('/dashboards/', 'POST', {
            name: 'Estimate Funnel & Campaigns',
            description: 'Dashboard for tracking UTM marketing campaigns and job wizard funnels.'
        });
        const dashboardId = dashboard.id;
        console.log(`✅ Dashboard created successfully! ID: ${dashboardId}`);

        console.log('📊 Creating conversion funnel insight...');
        await request('/insights/', 'POST', {
            dashboards: [dashboardId],
            name: 'Wizard Submission Funnel',
            query: {
                kind: 'InsightVizNode',
                source: {
                    kind: 'FunnelsQuery',
                    series: [
                        { kind: 'EventsNode', event: 'wizard_started', name: 'wizard_started' },
                        { kind: 'EventsNode', event: 'ai_estimate_generated', name: 'ai_estimate_generated' }
                    ],
                    funnelsFilter: {
                        funnelVizType: 'steps'
                    }
                }
            }
        });
        console.log('✅ Funnel insight added.');

        console.log('📈 Creating campaign conversion insight...');
        await request('/insights/', 'POST', {
            dashboards: [dashboardId],
            name: 'Submissions by UTM Campaign',
            query: {
                kind: 'InsightVizNode',
                source: {
                    kind: 'TrendsQuery',
                    series: [
                        { kind: 'EventsNode', event: 'ai_estimate_generated', math: 'dau' }
                    ],
                    breakdownFilter: {
                        breakdown: '$utm_campaign',
                        breakdown_type: 'event'
                    },
                    trendsFilter: {
                        display: 'ActionsBarValue'
                    }
                }
            }
        });
        console.log('✅ Campaign conversion insight added.');

        console.log('🌎 Creating acquisition sources insight...');
        await request('/insights/', 'POST', {
            dashboards: [dashboardId],
            name: 'Acquisition Sources (wizard_started)',
            query: {
                kind: 'InsightVizNode',
                source: {
                    kind: 'TrendsQuery',
                    series: [
                        { kind: 'EventsNode', event: 'wizard_started', math: 'dau' }
                    ],
                    breakdownFilter: {
                        breakdown: '$utm_source',
                        breakdown_type: 'event'
                    },
                    trendsFilter: {
                        display: 'ActionsPie'
                    }
                }
            }
        });
        console.log('✅ Acquisition sources insight added.');

        console.log(`\n🎉 All done! Visit your dashboard here: https://us.posthog.com/project/${PROJECT_ID}/dashboard/${dashboardId}`);
    } catch (e) {
        console.error('❌ Error creating dashboard:', e.message);
    }
}

run();


