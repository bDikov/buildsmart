const fs = require('fs');

const jsonContent = fs.readFileSync('Categories_Seed_Templates.json', 'utf8');
const data = JSON.parse(jsonContent);

let sql = '';

for (const key in data) {
    const category = data[key];
    const name = category.name.replace(/'/g, "''");
    const templateJson = JSON.stringify(category.templateStructure).replace(/'/g, "''");
    const isGlobal = key === 'global_category' ? 'true' : 'false';
    const status = 1; // 1 = Active, assuming enum CategoryStatus.Active = 1

    sql += `INSERT INTO "ServiceCategories" ("Id", "Name", "Status", "IsGlobal", "TemplateStructure", "CreatedAt", "UpdatedAt")
VALUES (gen_random_uuid(), '${name}', ${status}, ${isGlobal}, '${templateJson}'::jsonb, now(), now())
ON CONFLICT ("Name") 
DO UPDATE SET 
    "TemplateStructure" = EXCLUDED."TemplateStructure",
    "IsGlobal" = EXCLUDED."IsGlobal",
    "UpdatedAt" = now();\n\n`;
}

fs.writeFileSync('SeedLiveCategories.sql', sql, 'utf8');
console.log('SQL generated with UPSERT (INSERT ON CONFLICT) logic in UTF-8.');