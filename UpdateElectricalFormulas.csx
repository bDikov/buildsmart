#r "nuget: Npgsql, 7.0.4"
using System;
using Npgsql;

string connString = "Server=localhost;Port=5432;Database=buildsmart_db;Username=postgres;Password=postgres";
using (var conn = new NpgsqlConnection(connString)) {
    conn.Open();
    Console.WriteLine("Connected to database. Updating electrical formulas...");

    var queries = new[] {
        // Reset wrong updates from previous execution
        "UPDATE \"ServiceSkus\" SET \"CalculationFormula\" = '' WHERE \"SkuCode\" IN ('ELEC-021', 'ELEC-022', 'ELEC-023', 'ELEC-027', 'ELEC-028', 'ELEC-032', 'ELEC-062');",

        // Correct electrical formulas based on local.json SKU codes
        "UPDATE \"ServiceSkus\" SET \"CalculationFormula\" = 'if(Contains(global_wall_material, ''Тухла'') && !Contains(elec_scope, ''Само монтаж''), (if(Contains(elec_outlets_comfort, ''Премиум''), (global_room_count * 8) + 10, if(Contains(elec_outlets_comfort, ''Комфорт''), (global_room_count * 5) + 6, (global_room_count * 3) + 4)) + elec_lv_count + elec_dev_count + elec_spec_count), 0)' WHERE \"SkuCode\" = 'ELEC-084';",
        
        "UPDATE \"ServiceSkus\" SET \"CalculationFormula\" = 'if(Contains(global_wall_material, ''Бетон'') && !Contains(elec_scope, ''Само монтаж''), (if(Contains(elec_outlets_comfort, ''Премиум''), (global_room_count * 8) + 10, if(Contains(elec_outlets_comfort, ''Комфорт''), (global_room_count * 5) + 6, (global_room_count * 3) + 4)) + elec_lv_count + elec_dev_count + elec_spec_count), 0)' WHERE \"SkuCode\" = 'ELEC-085';",
        
        "UPDATE \"ServiceSkus\" SET \"CalculationFormula\" = 'if(!Contains(elec_scope, ''Само монтаж''), (if(Contains(elec_outlets_comfort, ''Премиум''), (global_room_count * 8) + 10, if(Contains(elec_outlets_comfort, ''Комфорт''), (global_room_count * 5) + 6, (global_room_count * 3) + 4)) + elec_lv_count + elec_dev_count + elec_spec_count), 0)' WHERE \"SkuCode\" = 'ELEC-020';",
        
        "UPDATE \"ServiceSkus\" SET \"CalculationFormula\" = 'if(Contains(elec_outlets_comfort, ''Премиум''), (global_room_count * 8) + 10, if(Contains(elec_outlets_comfort, ''Комфорт''), (global_room_count * 5) + 6, (global_room_count * 3) + 4))' WHERE \"SkuCode\" = 'ELEC-002';",
        
        "UPDATE \"ServiceSkus\" SET \"CalculationFormula\" = 'elec_dev_count + global_room_count + global_bathroom_count' WHERE \"SkuCode\" = 'ELEC-001';",
        
        "UPDATE \"ServiceSkus\" SET \"CalculationFormula\" = 'if(Contains(elec_lighting, ''Стандартно''), global_room_count + global_bathroom_count, 0)' WHERE \"SkuCode\" = 'ELEC-009';",
        
        "UPDATE \"ServiceSkus\" SET \"CalculationFormula\" = 'if(Contains(elec_lighting, ''лунички''), (global_room_count + global_bathroom_count) * 6, 0)' WHERE \"SkuCode\" = 'ELEC-010';",
        
        "UPDATE \"ServiceSkus\" SET \"CalculationFormula\" = 'if(!Contains(elec_scope, ''Само монтаж''), global_room_count + global_bathroom_count, 0)' WHERE \"SkuCode\" = 'ELEC-007';",
        
        "UPDATE \"ServiceSkus\" SET \"CalculationFormula\" = '(12 + Count(elec_heavy_appliances) + elec_ac_count) * 2' WHERE \"SkuCode\" = 'ELEC-037';",
        
        "UPDATE \"ServiceSkus\" SET \"CalculationFormula\" = 'if(Contains(elec_lighting, ''LED''), global_room_count, 0)' WHERE \"SkuCode\" = 'ELEC-LED-TRAFO';"
    };

    int totalUpdated = 0;
    foreach (var q in queries) {
        using (var cmd = new NpgsqlCommand(q, conn)) {
            totalUpdated += cmd.ExecuteNonQuery();
        }
    }
    Console.WriteLine($"Successfully updated {totalUpdated} SKU formulas in the database.");
}
