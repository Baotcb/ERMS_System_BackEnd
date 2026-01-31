using System;
using System.Collections.Generic;
using System.Linq;

namespace ERMS.Domain.Constants
{
    /// <summary>
    /// Danh sách tên doanh nghiệp lớn bị hạn chế sử dụng
    /// </summary>
    public static class ReservedEnterpriseNames
    {
        /// <summary>
        /// Danh sách tên các doanh nghiệp/tập đoàn lớn không được phép đăng ký
        /// </summary>
        public static readonly HashSet<string> BlacklistedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Tập đoàn công nghệ quốc tế
           "TSMC", "Cisco", "Dell", "HP", "Lenovo", "Qualcomm", "Broadcom", "ASML", 
"Texas Instruments", "Micron", "Foxconn", "Panasonic", "Canon", "Nintendo", 
"Nokia", "Ericsson", "Accenture", "Infosys", "Tata Consultancy Services",

// Ô tô, Máy bay & Công nghiệp nặng
"Toyota", "Volkswagen", "Honda", "Hyundai", "Kia", "BMW", "Mercedes-Benz", 
"Ford", "General Motors", "Nissan", "Audi", "Porsche", "Ferrari", "Lamborghini", 
"Boeing", "Airbus", "Lockheed Martin", "General Electric", "Siemens", "Bosch",
"Caterpillar", "John Deere", "3M", "Honeywell",

// Năng lượng & Dầu khí
"Saudi Aramco", "ExxonMobil", "Chevron", "Shell", "BP", "TotalEnergies", 
"Gazprom", "Sinopec", "PetroChina",

// Hàng tiêu dùng, Bán lẻ & Dược phẩm
"Walmart", "Costco", "Target", "IKEA", "Unilever", "P&G", "Procter & Gamble", 
"Nestlé", "Coca-Cola", "PepsiCo", "McDonald's", "Starbucks", "KFC", "Burger King",
"Nike", "Adidas", "LVMH", "Hermès", "Gucci", "Zara", "H&M", "Uniqlo",
"Johnson & Johnson", "Pfizer", "Moderna", "AstraZeneca", "GlaxoSmithKline",

// Tài chính, Bảo hiểm & Logistics (Quốc tế)
"Visa", "Mastercard", "American Express", "PayPal", "Stripe", "Square",
"Deloitte", "PwC", "EY", "KPMG", "McKinsey", "Boston Consulting Group",
"Berkshire Hathaway", "BlackRock", "Vanguard",
"Allianz", "AXA", "MetLife", "Prudential", "AIA", "Manulife", "Chubb", 
"Zurich Insurance", "Generali", "UBS", "Credit Suisse", "Barclays",
"FedEx", "UPS", "DHL", "Maersk", "MSC", "CMA CGM", "Delta Air Lines", 
"United Airlines", "Lufthansa", "Emirates", "Qatar Airways",

// Truyền thông, Giải trí & SaaS (Công cụ lập trình)
"The Walt Disney Company", "Disney", "Warner Bros. Discovery", "Paramount", 
"Universal Pictures", "Spotify", "Airbnb", "Booking.com", "Expedia", "eBay", 
"Rakuten", "Electronic Arts", "EA", "Activision Blizzard",
"GitHub", "GitLab", "Atlassian", "Jira", "Trello", "Slack", "Zoom", 
"Cloudflare", "DigitalOcean", "Heroku", "Vercel", "Netlify", "MongoDB Inc", 
"Redis Labs", "Docker", "Kubernetes", "HashiCorp", "JetBrains",


// --- DOANH NGHIỆP VIỆT NAM (BỔ SUNG) ---

// Ngân hàng (Ngoài Big4 và Techcom/VP/MB)
"VIB", "Quốc Tế", "SHB", "Sài Gòn - Hà Nội", "OCB", "Phương Đông", 
"MSB", "Hàng Hải", "Eximbank", "SeABank", "LPBank", "LienVietPostBank", 
"Nam A Bank", "PVcomBank", "Bac A Bank", "Vietbank", "Viet Capital Bank",

// Sản xuất, Thực phẩm & Nông nghiệp
"Vinamilk", "Sabeco", "Habeco", "Kido Group", "Nutifood", "TH True Milk",
"Dabaco", "Lộc Trời", "Loc Troi Group", "HAGL", "Hoang Anh Gia Lai", 
"HAGL Agrico", "Minh Phú", "Minh Phu Seafood", "Vĩnh Hoàn", "Vinh Hoan Corp",
"Cholimex", "Vissan", "Acecook Việt Nam", "Uniben", "Mì 3 Miền",
"Trung Nguyên", "Trung Nguyen Legend", "Highlands Coffee", "The Coffee House",
"Phúc Long", "Kichi Kichi", "Golden Gate Group", "Redsun",

// Năng lượng, Vật liệu xây dựng & Thép
"Petrolimex", "PV Gas", "PV Power", "BSR", "Lọc hóa dầu Bình Sơn", 
"Vinacomin", "TKV", "Than - Khoáng sản Việt Nam", "Đạm Phú Mỹ", "Đạm Cà Mau",
"Casumina", "Cao su Việt Nam", "VRG",
"Hoa Sen Group", "Tôn Hoa Sen", "Nam Kim Steel", "Thép Nam Kim", "Pomina",
"Vicostone", "Eurowindow", "Đồng Tâm Group",
"Nhựa Bình Minh", "Nhựa Tiền Phong", "An Phat Holdings",

// Bất động sản & Xây dựng (Ngoài Vin/Nova/Sun)
"Coteccons", "Hòa Bình", "Hoa Binh Construction", "Vinaconex", "Cienco4", 
"Gelex", "RE E Corp", "Cơ Điện Lạnh", "Kinh Bắc", "KBC", 
"Đất Xanh", "Dat Xanh Group", "Becamex", "VSIP", "Sonadezi", 
"Khang Điền", "Nam Long", "Hưng Thịnh", "Masterise Homes",
"Phú Mỹ Hưng", "Phu My Hung", "Sunshine Group", 
"Văn Phú Invest", "CEO Group", "Tập đoàn Đèo Cả", "Deo Ca Group", 
"Tasco", "Hutasco", "CenGroup",

// Công nghệ, Bán lẻ & Dịch vụ khác
"VNG", "VNG Corporation", "CMC Corp", "CMC Telecom", "Elcom", 
"FPT Retail", "Long Châu", "Pharmacity", "PNJ", "Doji", 
"Digiworld", "Petrosetco", "CellphoneS", "Hoàng Hà Mobile", "GearVN", 
"KidsPlaza", "Con Cưng", "Bibomart", "TokyoLife", "Canifa", "Biti's",
"Gemadept", "Vinalines", "VIMC", "Tổng công ty Hàng hải Việt Nam", 
"Vietnam Post", "VNPost", "Viettel Post", "Giao Hàng Tiết Kiệm", "GHTK", 
"F88", "Be Group", "Be",
"Bảo Việt", "Bao Viet Holdings", "PVI", "PTI",
// Từ khóa nhạy cảm
            "Government", "Chính phủ", "Ministry", "Bộ", "Police", "Công an",
            "Military", "Quân đội", "Army", "Navy", "Air Force",
            "United Nations", "Liên Hợp Quốc", "World Bank", "IMF",
            "Communist Party", "Đảng Cộng sản","Phục Quốc Trù Tử", "Phản Động lãnh địa", "Thanh Hóa"
        };

        /// <summary>
        /// Kiểm tra tên có nằm trong danh sách blacklist không
        /// </summary>
        public static bool IsBlacklisted(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            var normalizedName = name.Trim();

            // Kiểm tra exact match
            if (BlacklistedNames.Contains(normalizedName))
                return true;

            // Kiểm tra nếu tên chứa bất kỳ từ khóa blacklist nào
            foreach (var blacklisted in BlacklistedNames)
            {
                if (normalizedName.Contains(blacklisted, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
