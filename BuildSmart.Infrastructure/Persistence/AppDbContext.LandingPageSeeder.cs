using System;
using System.Linq;
using System.Threading.Tasks;
using BuildSmart.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuildSmart.Infrastructure.Persistence;

public static class LandingPageSeeder
{
    public static async Task SeedLandingPagesAsync(this AppDbContext context)
    {
        if (await context.LandingPages.AnyAsync())
        {
            return;
        }

        var defaultPages = new[]
        {
            new LandingPageContent
            {
                Id = Guid.NewGuid(),
                Slug = "remont-na-apartament-sofia",
                PageType = "apartment",
                TitleBg = "Цялостен Луксозен Ремонт на Апартамент в София",
                TitleEn = "Turnkey Luxury Apartment Renovation in Sofia",
                SubtitleBg = "Превърнете дома си в шедьовър с гарантирано качество, 3D визуализация, фиксиран бюджет по договор и 0 лв. аванс.",
                SubtitleEn = "Transform your apartment into a masterpiece with guaranteed quality, 3D design, fixed contract budget, and 0 BGN upfront.",
                BadgeBg = "Премиум Изпълнение 2026",
                BadgeEn = "Premium Quality 2026",
                HeroImageUrl = "https://images.unsplash.com/photo-1600585154340-be6161a56a0c?auto=format&fit=crop&w=1920&q=80",
                HeroVideoUrl = "https://assets.mixkit.co/videos/preview/mixkit-modern-apartment-interior-design-41558-large.mp4",
                MediaGalleryJson = "[{\"url\":\"https://images.unsplash.com/photo-1600585154340-be6161a56a0c?auto=format&fit=crop&w=800&q=80\",\"type\":\"image\",\"captionBg\":\"Цялостен ремонт на тристаен апартамент (88 кв.м)\",\"captionEn\":\"Turnkey 3-Room Apartment (88 sqm)\",\"locationBg\":\"София, кв. Манастирски Ливади\",\"durationBg\":\"42 работни дни\",\"budgetBg\":\"34,800 лв. (€17,793)\"},{\"url\":\"https://images.unsplash.com/photo-1584622650111-993a426fbf0a?auto=format&fit=crop&w=800&q=80\",\"type\":\"image\",\"captionBg\":\"Модерен ремонт на баня с крупноформатен гранитогрес (6 кв.м)\",\"captionEn\":\"Modern Bathroom Porcelain Renovation (6 sqm)\",\"locationBg\":\"София, кв. Лозенец\",\"durationBg\":\"11 работни дни\",\"budgetBg\":\"7,450 лв. (€3,809)\"},{\"url\":\"https://images.unsplash.com/photo-1513694203232-719a280e022f?auto=format&fit=crop&w=800&q=80\",\"type\":\"image\",\"captionBg\":\"Шпакловка, боядисване и гипсокартон на нов апартамент (65 кв.м)\",\"captionEn\":\"Plaster Skimming, Painting & Drywall (65 sqm)\",\"locationBg\":\"София, кв. Младост 4\",\"durationBg\":\"18 работни дни\",\"budgetBg\":\"14,200 лв. (€7,260)\"}]",
                FeaturesJson = "[{\"titleBg\":\"0 лв. Аванс\",\"titleEn\":\"0 BGN Advance\",\"descBg\":\"Заплащате само приключени и приети етапи с протокол.\",\"descEn\":\"Pay only for completed milestones signed off by you.\",\"icon\":\"shield\"},{\"titleBg\":\"Фиксирана Цена\",\"titleEn\":\"Fixed Price Guarantee\",\"descBg\":\"Без скрити такси. Офертата е окончателна по договор.\",\"descEn\":\"Zero hidden fees. Contract price is 100% locked.\",\"icon\":\"lock\"},{\"titleBg\":\"3 Мин. AI Оферта\",\"titleEn\":\"3 Min Instant AI Estimate\",\"descBg\":\"Мълниеносно пресмятане на количествено-стойностна сметка.\",\"descEn\":\"Instant itemized estimation with our AI Pricing Engine.\",\"icon\":\"clock\"}]",
                CtaTextBg = "Изчислете цена за Вашия апартамент",
                CtaTextEn = "Calculate estimate for your apartment",
                CtaLink = "/renovation-estimator",
                IsPublished = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new LandingPageContent
            {
                Id = Guid.NewGuid(),
                Slug = "remont-na-banya",
                PageType = "bathroom",
                TitleBg = "Модерен и Качествен Ремонт на Баня в София",
                TitleEn = "Modern & Premium Bathroom Renovation in Sofia",
                SubtitleBg = "Хидроизолация, лепене на плочки, подмяна на ВиК и монтаж на санитария с 5 години гаранция.",
                SubtitleEn = "2K Waterproofing, porcelain tiling, plumbing, and sanitary fitout with a 5-year written warranty.",
                BadgeBg = "5 Години Гаранция",
                BadgeEn = "5 Year Warranty",
                HeroImageUrl = "https://images.unsplash.com/photo-1584622650111-993a426fbf0a?auto=format&fit=crop&w=1920&q=80",
                HeroVideoUrl = "",
                MediaGalleryJson = "[{\"url\":\"https://images.unsplash.com/photo-1584622650111-993a426fbf0a?auto=format&fit=crop&w=800&q=80\",\"type\":\"image\",\"captionBg\":\"Модерен ремонт на баня с крупноформатен гранитогрес (6 кв.м)\",\"captionEn\":\"Modern Bathroom Porcelain Renovation (6 sqm)\",\"locationBg\":\"София, кв. Лозенец\",\"durationBg\":\"11 работни дни\",\"budgetBg\":\"7,450 лв. (€3,809)\",\"order\":1,\"section\":\"gallery\"}]",
                FeaturesJson = "[{\"titleBg\":\"2K Хидроизолация\",\"titleEn\":\"2K Waterproofing\",\"descBg\":\"Двукомпонентна защита от течове.\",\"descEn\":\"Double layer protection against leaks.\",\"icon\":\"droplet\"}]",
                CtaTextBg = "Изчислете цена за Вашата баня",
                CtaTextEn = "Calculate price for your bathroom",
                CtaLink = "/renovation-estimator?category=banya",
                IsPublished = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new LandingPageContent
            {
                Id = Guid.NewGuid(),
                Slug = "dovarshetelni-raboti",
                PageType = "finishing",
                TitleBg = "Довършителни Работи, Шпакловка & Боядисване",
                TitleEn = "Finishing Works, Plaster Skimming & Painting",
                SubtitleBg = "Перфектно гладки стени, гипсокартон, ламинат и боядисване с висококачествени материали.",
                SubtitleEn = "Flawless smooth walls, drywall ceilings, laminate flooring, and premium paint finish.",
                BadgeBg = "Премиум Изпълнение",
                BadgeEn = "Premium Execution",
                HeroImageUrl = "https://images.unsplash.com/photo-1513694203232-719a280e022f?auto=format&fit=crop&w=1920&q=80",
                HeroVideoUrl = "",
                MediaGalleryJson = "[]",
                FeaturesJson = "[{\"titleBg\":\"Q5 Шпакловка\",\"titleEn\":\"Q5 Skimming\",\"descBg\":\"Огледално гладки повърхности.\",\"descEn\":\"Mirror smooth wall surface finish.\",\"icon\":\"sparkles\"}]",
                CtaTextBg = "Изчислете довършителни работи",
                CtaTextEn = "Calculate finishing works",
                CtaLink = "/renovation-estimator?category=dovarshetelni",
                IsPublished = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new LandingPageContent
            {
                Id = Guid.NewGuid(),
                Slug = "el-i-vik-uslugi",
                PageType = "mep",
                TitleBg = "Електро и ВиК Инсталации в София",
                TitleEn = "Electrical & Plumbing Services in Sofia",
                SubtitleBg = "Ел. табла, окабеляване, водопровод и канализация от сертифицирани майстори.",
                SubtitleEn = "Distribution panels, wiring, plumbing supply, and drainage by certified master technicians.",
                BadgeBg = "Сертифицирани Майстори",
                BadgeEn = "Certified Technicians",
                HeroImageUrl = "https://images.unsplash.com/photo-1621905251189-08b45d6a269e?auto=format&fit=crop&w=1920&q=80",
                HeroVideoUrl = "",
                MediaGalleryJson = "[]",
                FeaturesJson = "[{\"titleBg\":\"Сертифицирано Окабеляване\",\"titleEn\":\"Certified Wiring\",\"descBg\":\"Отговарящо на всички EN стандарти.\",\"descEn\":\"Compliant with all EN safety standards.\",\"icon\":\"zap\"}]",
                CtaTextBg = "Изчислете ВиК и Ел. точка",
                CtaTextEn = "Calculate MEP points",
                CtaLink = "/renovation-estimator",
                IsPublished = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        await context.LandingPages.AddRangeAsync(defaultPages);
        await context.SaveChangesAsync();
    }
}
