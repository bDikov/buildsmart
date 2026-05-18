SELECT c."Name", count(s."Id") as sku_count
FROM "ServiceCategories" c
LEFT JOIN "ServiceSkus" s ON s."ServiceCategoryId" = c."Id"
GROUP BY c."Name";