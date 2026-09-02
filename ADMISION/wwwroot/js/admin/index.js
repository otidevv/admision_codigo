// Dashboard — primer paint server-rendered; filtros disparan AJAX a /admin/dashboard-data
// y se aplican client-side. La URL se mantiene sincronizada via history.replaceState.

(function () {
    const INITIAL = window.AdminDashboardData || {};

    const C = {
        p50: '#fef2f5', p100: '#fde6ec', p200: '#fbc0d0', p300: '#f999b4',
        p400: '#f76e98', p500: '#f54477', p600: '#f31a5b', p700: '#d10e49',
        p800: '#a00b38', p900: '#6e0827',
        s50: '#f3f2fc', s100: '#e7e5f9', s200: '#c7c2f0', s300: '#a79fe7',
        s400: '#8f85d8', s500: '#716aca', s600: '#5a52b8', s700: '#4a4399',
        s800: '#3a347a', s900: '#2a255b', s1000: '#0f172a',
        muted: '#5c6478', text: '#1a1f2e'
    };
    const CHART_COLORS = [
        '#06b6d4', '#f59e0b', '#10b981', '#ef4444', '#8b5cf6', '#0ea5e9',
        '#ec4899', '#14b8a6', '#f97316', '#6366f1', '#84cc16', '#e11d48',
        '#0891b2', '#d97706', '#059669', '#dc2626', '#7c3aed', '#0284c7',
        '#db2777', '#0d9488', '#ea580c', '#4f46e5', '#65a30d', '#be123c',
        '#0e7490', '#b45309', '#047857', '#b91c1c', '#6d28d9', '#0369a1',
        '#be185d', '#0f766e', '#c2410c', '#4338ca', '#4d7c0f', '#9f1239'
    ];

    const MAP_COLORS = [
        '#ef4444', '#06b6d4', '#f59e0b', '#8b5cf6', '#10b981', '#e11d48',
        '#0ea5e9', '#f97316', '#6366f1', '#14b8a6', '#ec4899', '#84cc16',
        '#dc2626', '#0891b2', '#d97706', '#7c3aed', '#059669', '#db2777',
        '#0284c7', '#ea580c', '#4f46e5', '#0d9488', '#b45309', '#6d28d9',
        '#b91c1c', '#0369a1', '#be185d', '#0f766e', '#c2410c', '#4338ca'
    ];
    const alpha = (hex, a) => hex + Math.round(a * 255).toString(16).padStart(2, '0');

    Chart.defaults.font.family = "'Inter', system-ui, sans-serif";
    Chart.defaults.color = C.muted;
    Chart.defaults.plugins.legend.labels.usePointStyle = true;

    const charts = { topics: null, modalidades: null, carreras: null, genero: null, edades: null, discapacidad: null, regiones: null, grades: null };
    const mapState = {
        peru: { map: null, layer: null, geo: null },
        world: { map: null, layer: null, geo: null }
    };

    let allTypeModalities = (INITIAL.filterOptions?.typeModalities || []).map(t => ({
        id: t.id, name: t.name, parentId: t.parentId
    }));

    function $bAll(name) { return document.querySelectorAll('[data-bind="' + name + '"]'); }
    function setText(name, value) { $bAll(name).forEach(el => el.textContent = value); }
    function setStyleWidth(name, pct) {
        document.querySelectorAll('[data-bind-style="' + name + '"]').forEach(el => {
            el.style.width = pct + '%';
        });
    }
    function escapeHtml(s) {
        return String(s ?? '').replace(/[&<>"']/g, c =>
            ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
    }
    function applyPalette() {
        document.querySelectorAll('[data-palette]').forEach(el => {
            const idx = parseInt(el.dataset.palette, 10);
            if (isNaN(idx)) return;
            const color = CHART_COLORS[idx % CHART_COLORS.length];
            const target = el.dataset.paletteTarget || 'bg';
            if (target === 'bg' || target === 'all') el.style.backgroundColor = color;
            if (target === 'text' || target === 'all') el.style.color = color;
        });
    }
    function fmt1(n) { return Number(n || 0).toFixed(1); }

    function animateCounter(el, target, durationMs = 800) {
        const from = parseFloat(el.textContent) || 0;
        if (from === target) return;
        const startTime = performance.now();
        function step(now) {
            const t = Math.min((now - startTime) / durationMs, 1);
            const easeT = 1 - Math.pow(1 - t, 3);
            const current = from + (target - from) * easeT;
            el.textContent = (target % 1 === 0) ? Math.floor(current) : current.toFixed(1);
            if (t < 1) requestAnimationFrame(step);
        }
        requestAnimationFrame(step);
    }

    function normalizeName(s) {
        if (!s) return '';
        return s.toString().normalize('NFD').replace(/[̀-ͯ]/g, '').toUpperCase().trim();
    }
    function peruMapColor(index) { return MAP_COLORS[index % MAP_COLORS.length]; }
    function worldMapColor(index) { return MAP_COLORS[index % MAP_COLORS.length]; }

    function initTopics(items) {
        const sorted = [...items].sort((a, b) => b.count - a.count);
        charts.topics = new Chart(document.getElementById('chartTopics'), {
            type: 'doughnut',
            data: {
                labels: sorted.map(x => 'Área ' + x.code),
                datasets: [{ data: sorted.map(x => x.count), backgroundColor: CHART_COLORS, borderWidth: 3, borderColor: '#fff', hoverOffset: 8 }]
            },
            options: {
                cutout: '65%', responsive: true, maintainAspectRatio: false,
                plugins: { legend: { position: 'bottom', labels: { font: { size: 10 } } } }
            }
        });
    }
    function initModalidades(chartData) {
        charts.modalidades = new Chart(document.getElementById('chartModalidades'), {
            type: 'bar',
            data: {
                labels: chartData.labels,
                datasets: [{
                    label: 'Postulantes', data: chartData.values,
                    backgroundColor: chartData.labels.map((_, i) => alpha(CHART_COLORS[i % CHART_COLORS.length], 0.85)),
                    borderRadius: 6
                }]
            },
            options: {
                indexAxis: 'y', responsive: true, maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: {
                    x: { grid: { color: C.s50 }, ticks: { font: { size: 10 } } },
                    y: { grid: { display: false }, ticks: { font: { size: 10 } } }
                }
            }
        });
    }
    function initCarreras(chartData) {
        charts.carreras = new Chart(document.getElementById('chartCarreras'), {
            type: 'bar',
            data: {
                labels: chartData.labels,
                datasets: [{ label: 'Postulantes', data: chartData.values, backgroundColor: chartData.values.map((_, i) => alpha(CHART_COLORS[i % CHART_COLORS.length], 0.85)), borderRadius: 5 }]
            },
            options: {
                responsive: true, maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: {
                    x: { grid: { display: false }, ticks: { font: { size: 8 }, maxRotation: 45 } },
                    y: { grid: { color: C.s50 }, ticks: { font: { size: 10 } }, beginAtZero: true }
                }
            }
        });
    }
    function initGenero(gender) {
        charts.genero = new Chart(document.getElementById('chartGenero'), {
            type: 'doughnut',
            data: {
                labels: ['Masculino', 'Femenino'],
                datasets: [{ data: [gender.male, gender.female], backgroundColor: ['#06b6d4', '#f59e0b'], borderWidth: 3, borderColor: '#fff', hoverOffset: 6 }]
            },
            options: { cutout: '75%', responsive: true, maintainAspectRatio: false, plugins: { legend: { display: false } } }
        });
    }
    function initEdades(ageGroups) {
        charts.edades = new Chart(document.getElementById('chartEdades'), {
            type: 'bar',
            data: {
                labels: ['Niños', 'Jóvenes', 'Adultos', 'Mayores'],
                datasets: [{
                    label: 'Postulantes',
                    data: [ageGroups.children, ageGroups.young, ageGroups.adult, ageGroups.senior],
                    backgroundColor: ['#ef4444', '#06b6d4', '#f59e0b', '#8b5cf6'],
                    borderRadius: 6
                }]
            },
            options: {
                responsive: true, maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: { x: { grid: { display: false } }, y: { grid: { color: C.s50 }, beginAtZero: true } }
            }
        });
    }
    function initDiscapacidad(disability) {
        const el = document.getElementById('chartDiscapacidad');
        if (!el) return;
        charts.discapacidad = new Chart(el, {
            type: 'doughnut',
            data: {
                labels: ['Visual', 'Auditiva', 'Motora', 'Intelectual', 'Otros'],
                datasets: [{
                    data: [disability.visual, disability.auditory, disability.motor, disability.intellectual, disability.other],
                    backgroundColor: ['#06b6d4', '#f59e0b', '#10b981', '#8b5cf6', '#94a3b8'],
                    borderWidth: 3, borderColor: '#fff', hoverOffset: 6
                }]
            },
            options: {
                cutout: '60%', responsive: true, maintainAspectRatio: false,
                plugins: { legend: { position: 'right', labels: { font: { size: 10 }, boxWidth: 10 } } }
            }
        });
    }
    function initGrades(chartData) {
        const el = document.getElementById('chartGrades');
        if (!el) return;
        if (!chartData.labels || chartData.labels.length === 0) {
            document.getElementById('gradeCard')?.classList.add('hidden');
            return;
        }
        document.getElementById('gradeCard')?.classList.remove('hidden');
        charts.grades = new Chart(el, {
            type: 'bar',
            data: {
                labels: chartData.labels,
                datasets: [{
                    label: 'Postulantes', data: chartData.values,
                    backgroundColor: chartData.labels.map((_, i) => alpha(CHART_COLORS[i % CHART_COLORS.length], 0.85)),
                    borderRadius: 6
                }]
            },
            options: {
                responsive: true, maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: {
                    x: { grid: { display: false }, ticks: { font: { size: 10 } } },
                    y: { grid: { color: C.s50 }, ticks: { font: { size: 10 } }, beginAtZero: true }
                }
            }
        });
    }

    function initRegiones(chartData) {
        charts.regiones = new Chart(document.getElementById('chartRegiones'), {
            type: 'doughnut',
            data: { labels: chartData.labels, datasets: [{ data: chartData.values, backgroundColor: CHART_COLORS, borderWidth: 2, borderColor: '#fff' }] },
            options: { responsive: true, maintainAspectRatio: false, plugins: { legend: { position: 'right', labels: { font: { size: 10 } } } } }
        });
    }

    async function initPeruMap(data) {
        const container = document.getElementById('peruMap');
        if (!container || typeof L === 'undefined') return;
        mapState.peru.map = L.map(container, { zoomControl: true, attributionControl: false, scrollWheelZoom: false }).setView([-9.19, -75.01], 5);
        try {
            const r = await fetch('/data/geo/peru-departments.geojson');
            if (!r.ok) return;
            mapState.peru.geo = await r.json();
        } catch (e) { return; }
        paintPeruMap(data);
        setTimeout(() => mapState.peru.map.invalidateSize(), 100);
    }
    function paintPeruMap(data) {
        if (!mapState.peru.map || !mapState.peru.geo) return;
        if (mapState.peru.layer) {
            mapState.peru.map.removeLayer(mapState.peru.layer);
            mapState.peru.layer = null;
        }
        const byName = new Map();
        const byCode = new Map();
        data.forEach(d => {
            byName.set(normalizeName(d.name), d.count);
            if (d.code) byCode.set(String(d.code).padStart(2, '0'), d.count);
        });
        const sortedData = [...data].sort((a, b) => b.count - a.count);
        const colorIndex = new Map();
        sortedData.forEach((d, i) => {
            colorIndex.set(normalizeName(d.name), i);
            if (d.code) colorIndex.set(String(d.code).padStart(2, '0'), i);
        });
        mapState.peru.layer = L.geoJSON(mapState.peru.geo, {
            style: (feature) => {
                const p = feature.properties || {};
                const count = byName.get(normalizeName(p.NOMBDEP))
                    ?? byCode.get(String(p.FIRST_IDDP || '').padStart(2, '0')) ?? 0;
                if (count <= 0) return { fillColor: '#eef0f4', weight: 1, color: '#bcc2cf', fillOpacity: 0.3 };
                const idx = colorIndex.get(normalizeName(p.NOMBDEP))
                    ?? colorIndex.get(String(p.FIRST_IDDP || '').padStart(2, '0')) ?? 0;
                return { fillColor: peruMapColor(idx), weight: 1, color: '#5c6478', fillOpacity: 0.85 };
            },
            onEachFeature: (feature, layer) => {
                const p = feature.properties || {};
                const count = byName.get(normalizeName(p.NOMBDEP))
                    ?? byCode.get(String(p.FIRST_IDDP || '').padStart(2, '0')) ?? 0;
                const name = p.NOMBDEP ? p.NOMBDEP.charAt(0) + p.NOMBDEP.slice(1).toLowerCase() : 'Departamento';
                layer.bindTooltip('<strong>' + name + '</strong><br>Postulantes: <b>' + count + '</b>', { sticky: true });
                layer.on('mouseover', () => layer.setStyle({ weight: 2, color: '#0e1220' }));
                layer.on('mouseout', () => layer.setStyle({ weight: 1, color: '#5c6478' }));
            }
        }).addTo(mapState.peru.map);
    }

    const ISO2_TO_ISO3 = {
        AF: 'AFG', AL: 'ALB', DZ: 'DZA', AS: 'ASM', AD: 'AND', AO: 'AGO', AI: 'AIA', AQ: 'ATA', AG: 'ATG', AR: 'ARG',
        AM: 'ARM', AW: 'ABW', AU: 'AUS', AT: 'AUT', AZ: 'AZE', BS: 'BHS', BH: 'BHR', BD: 'BGD', BB: 'BRB', BY: 'BLR',
        BE: 'BEL', BZ: 'BLZ', BJ: 'BEN', BM: 'BMU', BT: 'BTN', BO: 'BOL', BA: 'BIH', BW: 'BWA', BV: 'BVT', BR: 'BRA',
        IO: 'IOT', BN: 'BRN', BG: 'BGR', BF: 'BFA', BI: 'BDI', KH: 'KHM', CM: 'CMR', CA: 'CAN', CV: 'CPV', KY: 'CYM',
        CF: 'CAF', TD: 'TCD', CL: 'CHL', CN: 'CHN', CX: 'CXR', CC: 'CCK', CO: 'COL', KM: 'COM', CG: 'COG', CD: 'COD',
        CK: 'COK', CR: 'CRI', CI: 'CIV', HR: 'HRV', CU: 'CUB', CW: 'CUW', CY: 'CYP', CZ: 'CZE', DK: 'DNK', DJ: 'DJI',
        DM: 'DMA', DO: 'DOM', EC: 'ECU', EG: 'EGY', SV: 'SLV', GQ: 'GNQ', ER: 'ERI', EE: 'EST', ET: 'ETH', FK: 'FLK',
        FO: 'FRO', FJ: 'FJI', FI: 'FIN', FR: 'FRA', GF: 'GUF', PF: 'PYF', TF: 'ATF', GA: 'GAB', GM: 'GMB', GE: 'GEO',
        DE: 'DEU', GH: 'GHA', GI: 'GIB', GR: 'GRC', GL: 'GRL', GD: 'GRD', GP: 'GLP', GU: 'GUM', GT: 'GTM', GG: 'GGY',
        GN: 'GIN', GW: 'GNB', GY: 'GUY', HT: 'HTI', HM: 'HMD', VA: 'VAT', HN: 'HND', HK: 'HKG', HU: 'HUN', IS: 'ISL',
        IN: 'IND', ID: 'IDN', IR: 'IRN', IQ: 'IRQ', IE: 'IRL', IM: 'IMN', IL: 'ISR', IT: 'ITA', JM: 'JAM', JP: 'JPN',
        JE: 'JEY', JO: 'JOR', KZ: 'KAZ', KE: 'KEN', KI: 'KIR', KP: 'PRK', KR: 'KOR', KW: 'KWT', KG: 'KGZ', LA: 'LAO',
        LV: 'LVA', LB: 'LBN', LS: 'LSO', LR: 'LBR', LY: 'LBY', LI: 'LIE', LT: 'LTU', LU: 'LUX', MO: 'MAC', MK: 'MKD',
        MG: 'MDG', MW: 'MWI', MY: 'MYS', MV: 'MDV', ML: 'MLI', MT: 'MLT', MH: 'MHL', MQ: 'MTQ', MR: 'MRT', MU: 'MUS',
        YT: 'MYT', MX: 'MEX', FM: 'FSM', MD: 'MDA', MC: 'MCO', MN: 'MNG', ME: 'MNE', MS: 'MSR', MA: 'MAR', MZ: 'MOZ',
        MM: 'MMR', NA: 'NAM', NR: 'NRU', NP: 'NPL', NL: 'NLD', NC: 'NCL', NZ: 'NZL', NI: 'NIC', NE: 'NER', NG: 'NGA',
        NU: 'NIU', NF: 'NFK', MP: 'MNP', NO: 'NOR', OM: 'OMN', PK: 'PAK', PW: 'PLW', PS: 'PSE', PA: 'PAN', PG: 'PNG',
        PY: 'PRY', PE: 'PER', PH: 'PHL', PN: 'PCN', PL: 'POL', PT: 'PRT', PR: 'PRI', QA: 'QAT', RE: 'REU', RO: 'ROU',
        RU: 'RUS', RW: 'RWA', BL: 'BLM', SH: 'SHN', KN: 'KNA', LC: 'LCA', MF: 'MAF', PM: 'SPM', VC: 'VCT', WS: 'WSM',
        SM: 'SMR', ST: 'STP', SA: 'SAU', SN: 'SEN', RS: 'SRB', SC: 'SYC', SL: 'SLE', SG: 'SGP', SX: 'SXM', SK: 'SVK',
        SI: 'SVN', SB: 'SLB', SO: 'SOM', ZA: 'ZAF', GS: 'SGS', SS: 'SSD', ES: 'ESP', LK: 'LKA', SD: 'SDN', SR: 'SUR',
        SJ: 'SJM', SZ: 'SWZ', SE: 'SWE', CH: 'CHE', SY: 'SYR', TW: 'TWN', TJ: 'TJK', TZ: 'TZA', TH: 'THA', TL: 'TLS',
        TG: 'TGO', TK: 'TKL', TO: 'TON', TT: 'TTO', TN: 'TUN', TR: 'TUR', TM: 'TKM', TC: 'TCA', TV: 'TUV', UG: 'UGA',
        UA: 'UKR', AE: 'ARE', GB: 'GBR', US: 'USA', UM: 'UMI', UY: 'URY', UZ: 'UZB', VU: 'VUT', VE: 'VEN', VN: 'VNM',
        VG: 'VGB', VI: 'VIR', WF: 'WLF', EH: 'ESH', YE: 'YEM', ZM: 'ZMB', ZW: 'ZWE'
    };

    async function initWorldMap(data) {
        const container = document.getElementById('worldMap');
        if (!container || typeof L === 'undefined') return;
        mapState.world.map = L.map(container, { zoomControl: true, attributionControl: false, scrollWheelZoom: false, worldCopyJump: true }).setView([15, 0], 1);
        try {
            const r = await fetch('/data/geo/world-countries.geojson');
            if (!r.ok) return;
            mapState.world.geo = await r.json();
        } catch (e) { return; }
        paintWorldMap(data);
        setTimeout(() => mapState.world.map.invalidateSize(), 100);
    }
    function paintWorldMap(data) {
        if (!mapState.world.map || !mapState.world.geo) return;
        if (mapState.world.layer) {
            mapState.world.map.removeLayer(mapState.world.layer);
            mapState.world.layer = null;
        }
        const byIso3 = new Map();
        data.forEach(d => {
            const iso3 = ISO2_TO_ISO3[(d.code || '').toUpperCase()];
            if (iso3) byIso3.set(iso3, { name: d.name, count: d.count });
        });
        const sortedData = [...data].sort((a, b) => b.count - a.count);
        const colorIndex = new Map();
        sortedData.forEach((d, i) => {
            const iso3 = ISO2_TO_ISO3[(d.code || '').toUpperCase()];
            if (iso3) colorIndex.set(iso3, i);
        });
        mapState.world.layer = L.geoJSON(mapState.world.geo, {
            style: (feature) => {
                const iso3 = feature.id;
                const entry = byIso3.get(iso3);
                const count = entry?.count ?? 0;
                if (count <= 0) return { fillColor: '#eef0f4', weight: 0.5, color: '#bcc2cf', fillOpacity: 0.25 };
                const idx = colorIndex.get(iso3) ?? 0;
                return { fillColor: worldMapColor(idx), weight: 0.5, color: '#5c6478', fillOpacity: 0.85 };
            },
            onEachFeature: (feature, layer) => {
                const iso3 = feature.id;
                const entry = byIso3.get(iso3);
                const name = entry?.name || feature.properties?.name || 'País';
                const count = entry?.count ?? 0;
                layer.bindTooltip('<strong>' + name + '</strong><br>Postulantes: <b>' + count + '</b>', { sticky: true });
                layer.on('mouseover', () => layer.setStyle({ weight: 1.5, color: '#0e1220' }));
                layer.on('mouseout', () => layer.setStyle({ weight: 0.5, color: '#5c6478' }));
            }
        }).addTo(mapState.world.map);
    }

    // applyDto(dto): punto único donde el DTO recibido por AJAX se vuelca sobre
    // gráficos, contadores, tablas, badges y filtros.
    function applyDto(dto) {
        document.querySelectorAll('[data-kpi]').forEach(el => {
            const key = el.dataset.kpi;
            const v = key === 'totalPostulants' ? dto.totalPostulants
                : key === 'activeCareers' ? dto.activeCareers
                    : key === 'activeModalities' ? dto.activeModalities
                        : key === 'avgAge' ? Math.round(dto.avgAge)
                            : 0;
            el.dataset.target = v;
            animateCounter(el, v);
        });

        setText('topicsTotal', dto.topics.total);
        setText('topicsTotalCenter', dto.topics.total);
        renderTopicsList(dto.topics.items);
        if (charts.topics) {
            const sorted = [...dto.topics.items].sort((a, b) => b.count - a.count);
            charts.topics.data.labels = sorted.map(x => 'Área ' + x.code);
            charts.topics.data.datasets[0].data = sorted.map(x => x.count);
            charts.topics.update();
        }

        if (charts.modalidades) {
            charts.modalidades.data.labels = dto.modalitiesChart.labels;
            charts.modalidades.data.datasets[0].data = dto.modalitiesChart.values;
            charts.modalidades.data.datasets[0].backgroundColor =
                dto.modalitiesChart.labels.map((_, i) => alpha(CHART_COLORS[i % CHART_COLORS.length], 0.85));
            charts.modalidades.update();
        }

        if (charts.carreras) {
            charts.carreras.data.labels = dto.careersChart.labels;
            charts.carreras.data.datasets[0].data = dto.careersChart.values;
            charts.carreras.data.datasets[0].backgroundColor = dto.careersChart.values.map((_, i) => alpha(CHART_COLORS[i % CHART_COLORS.length], 0.85));
            charts.carreras.update();
        }

        const gTotal = (dto.gender.male || 0) + (dto.gender.female || 0);
        setText('genderTotal', gTotal);
        setText('genderMale', dto.gender.male);
        setText('genderFemale', dto.gender.female);
        setText('genderMalePct', fmt1(dto.gender.malePercentage) + '%');
        setText('genderFemalePct', fmt1(dto.gender.femalePercentage) + '%');
        if (charts.genero) {
            charts.genero.data.datasets[0].data = [dto.gender.male, dto.gender.female];
            charts.genero.update();
        }

        setText('schoolsPublic', dto.schools.public);
        setText('schoolsPrivate', dto.schools.private);
        setText('schoolsPublicPctText', fmt1(dto.schools.publicPercentage) + '%');
        setText('schoolsPrivatePctText', fmt1(dto.schools.privatePercentage) + '%');
        setStyleWidth('schoolsPublicPct', dto.schools.publicPercentage);
        setStyleWidth('schoolsPrivatePct', dto.schools.privatePercentage);

        setText('disabilityTotalUnique', dto.disability.totalUnique);
        const tot = Math.max(1, dto.totalPostulants);
        setText('disabilityPct', fmt1((dto.disability.totalUnique / tot) * 100) + '% del total de postulantes');
        [
            ['visual', dto.disability.visual, dto.disability.visualPct],
            ['auditory', dto.disability.auditory, dto.disability.auditoryPct],
            ['motor', dto.disability.motor, dto.disability.motorPct],
            ['intellectual', dto.disability.intellectual, dto.disability.intellectualPct],
            ['other', dto.disability.other, dto.disability.otherPct]
        ].forEach(([k, v, p]) => {
            setText('disability-' + k + '-value', v);
            setStyleWidth('disability-' + k + '-pct', p);
        });
        if (charts.discapacidad) {
            charts.discapacidad.data.datasets[0].data = [
                dto.disability.visual, dto.disability.auditory, dto.disability.motor,
                dto.disability.intellectual, dto.disability.other
            ];
            charts.discapacidad.update();
        }

        [
            ['children', dto.ageGroups.children, dto.ageGroups.childrenPercentage],
            ['young', dto.ageGroups.young, dto.ageGroups.youngPercentage],
            ['adult', dto.ageGroups.adult, dto.ageGroups.adultPercentage],
            ['senior', dto.ageGroups.senior, dto.ageGroups.seniorPercentage]
        ].forEach(([k, v, p]) => {
            setText('age-' + k + '-value', v);
            setText('age-' + k + '-pctText', fmt1(p) + '%');
            setStyleWidth('age-' + k + '-pct', p);
        });
        if (charts.edades) {
            charts.edades.data.datasets[0].data = [dto.ageGroups.children, dto.ageGroups.young, dto.ageGroups.adult, dto.ageGroups.senior];
            charts.edades.update();
        }

        if (charts.regiones) {
            charts.regiones.data.labels = dto.regionsChart.labels;
            charts.regiones.data.datasets[0].data = dto.regionsChart.values;
            charts.regiones.update();
        }

        const gc = dto.gradeDistribution;
        if (charts.grades) {
            if (gc.labels && gc.labels.length) {
                document.getElementById('gradeCard')?.classList.remove('hidden');
                charts.grades.data.labels = gc.labels;
                charts.grades.data.datasets[0].data = gc.values;
                charts.grades.data.datasets[0].backgroundColor =
                    gc.labels.map((_, i) => alpha(CHART_COLORS[i % CHART_COLORS.length], 0.85));
                charts.grades.update();
            } else {
                document.getElementById('gradeCard')?.classList.add('hidden');
            }
        }
        const pillReg = document.getElementById('pillRegiones');
        if (pillReg) {
            pillReg.innerHTML = dto.regionsChart.labels.map((lab, i) =>
                '<span class="badge b-secondary">' + escapeHtml(lab) + ': ' + dto.regionsChart.values[i] + '</span>'
            ).join('');
        }
        applyPalette();

        paintPeruMap(dto.peruMap);
        paintWorldMap(dto.worldMap);
        const peruTotal = dto.peruMap.reduce((s, d) => s + d.count, 0);
        const worldTotal = dto.worldMap.reduce((s, d) => s + d.count, 0);
        setText('peruTotal', peruTotal);
        setText('worldTotal', worldTotal);
        setText('termNamePeru', dto.selectedTermName);
        setText('termNameWorld', dto.selectedTermName);
        document.getElementById('peruMapEmpty')?.classList.toggle('hidden', dto.peruMap.length > 0);
        const pills = document.getElementById('worldPills');
        if (pills) {
            if (dto.worldMap.length === 0) {
                pills.classList.add('hidden');
                pills.innerHTML = '';
            } else {
                pills.classList.remove('hidden');
                pills.innerHTML = dto.worldMap.slice(0, 12).map(c =>
                    '<span class="badge b-primary">' + escapeHtml(c.name) + ': ' + c.count + '</span>'
                ).join('');
            }
        }
        document.getElementById('worldMapEmpty')?.classList.toggle('hidden', dto.worldMap.length > 0);

        renderTrasladosTable(dto.traslados);
        renderTransfersTable(dto.transfers);

        const periodLabel = document.getElementById('period-label');
        if (periodLabel) periodLabel.textContent = 'Mostrando: ' + dto.selectedTermName;

        refreshFilterOptions(dto);
        refreshFilterChips(dto);
    }

    function renderTopicsList(items) {
        const cont = document.getElementById('topicsList');
        if (!cont) return;
        if (!items || items.length === 0) {
            cont.innerHTML = '<div class="text-center py-8 text-ink-400 text-sm">No se han configurado áreas temáticas.</div>';
            return;
        }
        const sorted = [...items].sort((a, b) => b.count - a.count);
        cont.innerHTML = sorted.map((item, i) =>
            '<div class="bg-ink-50 dark:bg-ink-800/40 rounded-md p-3 ring-1 ring-ink-200/60 dark:ring-ink-800 flex items-center gap-3 hover:ring-secondary-300 transition-all">' +
            '<div class="w-9 h-9 rounded-md bg-white dark:bg-ink-900 ring-1 ring-ink-200/60 dark:ring-ink-800 flex items-center justify-center">' +
            '<span class="text-[10.5px] font-bold" data-palette="' + i + '" data-palette-target="text">' + escapeHtml(item.code) + '</span>' +
            '</div>' +
            '<div class="flex-1 min-w-0">' +
            '<div class="flex justify-between items-center mb-1">' +
            '<span class="text-[10px] font-bold text-ink-600 dark:text-ink-300 uppercase tracking-[0.14em]">Área ' + escapeHtml(item.code) + '</span>' +
            '<span class="text-xs font-mono font-bold text-ink-900 dark:text-ink-100 tabular-nums">' + item.count + '</span>' +
            '</div>' +
            '<div class="pbar"><div data-palette="' + i + '" data-palette-target="bg" style="width:' + fmt1(item.percentage) + '%"></div></div>' +
            '<div class="mt-1 text-right"><span class="text-[9px] font-bold text-ink-400">' + fmt1(item.percentage) + '%</span></div>' +
            '</div>' +
            '</div>'
        ).join('');
        applyPalette();
    }

    function renderTrasladosTable(rows) {
        const tbody = document.getElementById('trasladosTbody');
        if (!tbody) return;
        if (!rows || rows.length === 0) {
            tbody.innerHTML = '<tr><td colspan="5" class="px-4 py-6 text-center text-ink-400 italic">Sin datos de traslados.</td></tr>';
            return;
        }
        tbody.innerHTML = rows.map((item, i) =>
            '<tr>' +
            '<td class="font-mono font-bold text-secondary-500 tabular-nums">' + String(i + 1).padStart(2, '0') + '</td>' +
            '<td class="font-semibold text-ink-900 dark:text-ink-100">' + escapeHtml(item.university) + '</td>' +
            '<td class="text-center"><span class="badge b-secondary">' + item.external + '</span></td>' +
            '<td class="text-center"><span class="badge b-primary">' + item.internal + '</span></td>' +
            '<td class="text-right font-bold text-ink-900 dark:text-ink-100 tabular-nums">' + item.total + '</td>' +
            '</tr>'
        ).join('');
    }

    function renderTransfersTable(rows) {
        const tbody = document.getElementById('transfersTbody');
        if (!tbody) return;
        if (!rows || rows.length === 0) {
            tbody.innerHTML = '<tr><td colspan="7" class="px-6 py-8 text-center text-ink-400 italic">No hay transferencias registradas para este periodo.</td></tr>';
            return;
        }
        tbody.innerHTML = rows.map((item, i) => {
            const dateStr = new Date(item.date).toLocaleString('es-PE', {
                day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit'
            });
            return '<tr>' +
                '<td><span class="font-mono font-bold text-secondary-500 tabular-nums">' + String(i + 1).padStart(2, '0') + '</span></td>' +
                '<td>' +
                '<div class="font-bold text-ink-900 dark:text-ink-100">' + escapeHtml(item.fullName) + '</div>' +
                '<div class="text-[11px] text-ink-400">' + escapeHtml(item.email) + '</div>' +
                '</td>' +
                '<td class="font-mono text-xs text-ink-600 dark:text-ink-300 tabular-nums">' + escapeHtml(item.dni) + '</td>' +
                '<td>' +
                '<div class="flex items-center gap-2">' +
                '<span class="w-1.5 h-1.5 rounded-full bg-secondary-400"></span>' +
                '<span class="text-xs font-semibold text-secondary-700 dark:text-secondary-300">' + escapeHtml(item.operationCode) + '</span>' +
                '</div>' +
                '<div class="text-[11px] text-ink-500">' + escapeHtml(item.bankName) + '</div>' +
                '</td>' +
                '<td class="text-right"><span class="font-mono font-bold text-ink-900 dark:text-ink-100 text-sm tabular-nums">S/ ' + item.amount.toFixed(2) + '</span></td>' +
                '<td class="text-xs text-ink-500 tabular-nums">' + dateStr + '</td>' +
                '<td class="text-center"><span class="badge b-amber">Pendiente</span></td>' +
                '</tr>';
        }).join('');
    }

    function refreshFilterOptions(dto) {
        allTypeModalities = (dto.filterOptions?.typeModalities || []).map(t => ({
            id: t.id, name: t.name, parentId: t.parentId
        }));
        const setOptions = (selId, list) => {
            const reg = window.customSelectRegistry?.[selId];
            if (reg) reg.setOptions(list.map(x => ({ id: x.id, name: x.name })), true);
        };
        setOptions('filterModalityId', dto.filterOptions?.modalities || []);
        setOptions('filterCareerId', dto.filterOptions?.careers || []);
        setOptions('filterTematicAreaId', dto.filterOptions?.tematicAreas || []);
        applyTypeModalityState('refresh');
    }

    function refreshFilterChips(dto) {
        const cont = document.getElementById('filterChips');
        const clearBtn = document.getElementById('clearFilters');
        if (!cont) return;
        const chips = [];
        if (dto.selectedModalityId) {
            const m = (dto.filterOptions?.modalities || []).find(x => x.id === dto.selectedModalityId);
            chips.push('<span class="badge b-primary">Modalidad: ' + escapeHtml(m?.name || '—') + '</span>');
        }
        if (dto.selectedTypeModalityId) {
            const t = (dto.filterOptions?.typeModalities || []).find(x => x.id === dto.selectedTypeModalityId);
            chips.push('<span class="badge b-secondary">Tipo: ' + escapeHtml(t?.name || '—') + '</span>');
        }
        if (dto.selectedCareerId) {
            const c = (dto.filterOptions?.careers || []).find(x => x.id === dto.selectedCareerId);
            chips.push('<span class="badge b-blue">Carrera: ' + escapeHtml(c?.name || '—') + '</span>');
        }
        if (dto.selectedTematicAreaId) {
            const a = (dto.filterOptions?.tematicAreas || []).find(x => x.id === dto.selectedTematicAreaId);
            chips.push('<span class="badge b-violet">' + escapeHtml(a?.name || '—') + '</span>');
        }
        if (chips.length) {
            cont.classList.remove('hidden');
            cont.innerHTML = '<span class="eyebrow text-[10px]">Activos:</span>' + chips.join('');
            clearBtn?.classList.remove('hidden');
        } else {
            cont.classList.add('hidden');
            cont.innerHTML = '';
            clearBtn?.classList.add('hidden');
        }
    }

    function applyTypeModalityState(reason) {
        const reg = window.customSelectRegistry?.['filterTypeModalityId'];
        const btn = document.getElementById('btn_filterTypeModalityId');
        const display = document.getElementById('display_filterTypeModalityId');
        if (!reg || !btn || !display) return;
        const modId = document.getElementById('filterModalityId')?.value || '';
        const disabledCls = ['opacity-60', 'cursor-not-allowed', 'pointer-events-none', 'bg-ink-50'];
        btn.classList.remove(...disabledCls);
        if (!modId) {
            reg.setOptions([], false);
            btn.classList.add(...disabledCls);
            display.textContent = 'Selecciona una modalidad primero';
            display.classList.add('text-ink-400');
            display.classList.remove('text-ink-900', 'font-medium');
            return;
        }
        const filtered = allTypeModalities.filter(t => t.parentId === modId);
        if (filtered.length === 0) {
            reg.setOptions([], false);
            btn.classList.add(...disabledCls);
            display.textContent = 'Sin tipos de modalidad';
            display.classList.add('text-ink-400');
            display.classList.remove('text-ink-900', 'font-medium');
            return;
        }
        reg.setOptions(filtered.map(t => ({ id: t.id, name: t.name })), reason !== 'modality-change');
    }

    function getFilterValues() {
        return {
            termId: document.getElementById('filterTermId')?.value || '',
            modalityId: document.getElementById('filterModalityId')?.value || '',
            typeModalityId: document.getElementById('filterTypeModalityId')?.value || '',
            careerId: document.getElementById('filterCareerId')?.value || '',
            tematicAreaId: document.getElementById('filterTematicAreaId')?.value || ''
        };
    }

    let inflightController = null;
    async function loadDashboard() {
        const filters = getFilterValues();
        const params = new URLSearchParams();
        Object.entries(filters).forEach(([k, v]) => { if (v) params.set(k, v); });

        const qs = params.toString();
        history.replaceState(null, '', '/admin' + (qs ? '?' + qs : ''));

        if (inflightController) inflightController.abort();
        inflightController = new AbortController();

        const overlay = document.getElementById('dashboardLoading');
        overlay?.classList.remove('hidden');

        try {
            const resp = await fetch('/admin/dashboard-data?' + qs, {
                credentials: 'same-origin',
                signal: inflightController.signal
            });
            if (!resp.ok) throw new Error('HTTP ' + resp.status);
            const dto = await resp.json();
            applyDto(dto);
        } catch (e) {
            if (e.name === 'AbortError') return;
            console.error('Dashboard fetch failed', e);
            if (typeof toastError === 'function') {
                toastError('No se pudieron actualizar las estadísticas.');
            }
        } finally {
            overlay?.classList.add('hidden');
        }
    }

    document.addEventListener('DOMContentLoaded', () => {
        initTopics(INITIAL.topics.items);
        initModalidades(INITIAL.modalitiesChart);
        initCarreras(INITIAL.careersChart);
        initGenero(INITIAL.gender);
        initEdades(INITIAL.ageGroups);
        initDiscapacidad(INITIAL.disability);
        initRegiones(INITIAL.regionsChart);
        initGrades(INITIAL.gradeDistribution);
        initPeruMap(INITIAL.peruMap);
        initWorldMap(INITIAL.worldMap);

        applyPalette();

        document.querySelectorAll('.counter').forEach(el => {
            animateCounter(el, parseFloat(el.dataset.target || '0'), 1500);
        });

        applyTypeModalityState('init');

        const filterIds = ['filterTermId', 'filterModalityId', 'filterTypeModalityId', 'filterCareerId', 'filterTematicAreaId'];
        const lastValues = {};
        filterIds.forEach(id => { lastValues[id] = document.getElementById(id)?.value || ''; });

        filterIds.forEach(id => {
            const el = document.getElementById(id);
            if (!el) return;
            el.addEventListener('change', () => {
                const curr = el.value || '';
                if (curr === (lastValues[id] || '')) return;
                lastValues[id] = curr;

                if (id === 'filterTermId') {
                    ['filterModalityId', 'filterTypeModalityId', 'filterCareerId', 'filterTematicAreaId'].forEach(child => {
                        window.customSelectRegistry?.[child]?.clear();
                        lastValues[child] = '';
                    });
                } else if (id === 'filterModalityId') {
                    window.customSelectRegistry?.['filterTypeModalityId']?.clear();
                    lastValues['filterTypeModalityId'] = '';
                    applyTypeModalityState('modality-change');
                }

                loadDashboard();
            });
        });

        document.getElementById('clearFilters')?.addEventListener('click', (e) => {
            e.preventDefault();
            ['filterModalityId', 'filterTypeModalityId', 'filterCareerId', 'filterTematicAreaId'].forEach(id => {
                window.customSelectRegistry?.[id]?.clear();
                lastValues[id] = '';
            });
            applyTypeModalityState('init');
            loadDashboard();
        });
    });
})();
