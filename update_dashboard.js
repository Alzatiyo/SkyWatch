const fs = require('fs');
let data = JSON.parse(fs.readFileSync('observability/grafana/provisioning/dashboards/medipass-dashboard.json', 'utf8'));

data.panels.forEach(panel => {
    if (panel.targets) {
        let hasGw = panel.targets.some(t => t.expr && t.expr.includes('ms-apigateway'));
        if (!hasGw && panel.targets.length > 0) {
            let t0 = JSON.parse(JSON.stringify(panel.targets[0]));
            if (t0.expr && !t0.expr.includes('medipass_')) {
                t0.expr = t0.expr.replace('ms-agendahub', 'ms-apigateway');
                if (t0.legendFormat) {
                    t0.legendFormat = t0.legendFormat.replace('AgendaHub', 'ApiGateway');
                }
                t0.refId = String.fromCharCode(panel.targets[panel.targets.length-1].refId.charCodeAt(0) + 1);
                panel.targets.push(t0);
            }
        }
        
        if (panel.title && panel.title.includes('Tasa de Errores')) {
            panel.title = panel.title.replace('Tasa', 'Conteo');
            if (panel.fieldConfig && panel.fieldConfig.defaults) {
                panel.fieldConfig.defaults.unit = 'short';
            }
            panel.targets.forEach(t => {
                if (t.expr && t.expr.includes('rate(')) {
                    t.expr = t.expr.replace('rate(', 'increase(').replace('[1m]', '[5m]');
                }
            });
        }
    }
});

fs.writeFileSync('observability/grafana/provisioning/dashboards/medipass-dashboard.json', JSON.stringify(data, null, 2));
console.log('Done');
