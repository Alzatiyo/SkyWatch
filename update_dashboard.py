import json

with open('observability/grafana/provisioning/dashboards/medipass-dashboard.json', 'r', encoding='utf-8') as f:
    dashboard = json.load(f)

for panel in dashboard.get('panels', []):
    if 'targets' in panel:
        # Check if ms-apigateway is already there to avoid duplicates
        has_gateway = any('ms-apigateway' in t.get('expr', '') for t in panel['targets'])
        
        if not has_gateway:
            # We want to clone the first target but change the job and legend
            if len(panel['targets']) > 0:
                import copy
                new_target = copy.deepcopy(panel['targets'][0])
                
                # Replace job and legend
                new_target['expr'] = new_target['expr'].replace('ms-agendahub', 'ms-apigateway')
                new_target['legendFormat'] = new_target['legendFormat'].replace('AgendaHub', 'ApiGateway')
                new_target['refId'] = chr(ord(panel['targets'][-1]['refId']) + 1) if len(panel['targets'][-1]['refId']) == 1 else 'D'
                
                # Special cases for business metrics (they don't use job label usually)
                if 'medipass_' not in new_target['expr']:
                    panel['targets'].append(new_target)
        
        # Change rate to increase for 4xx and 5xx errors
        if panel.get('title', '').startswith('Tasa de Errores'):
            panel['title'] = panel['title'].replace('Tasa', 'Conteo')
            for t in panel['targets']:
                if 'rate(' in t['expr']:
                    t['expr'] = t['expr'].replace('rate(', 'increase(').replace('[1m]', '[5m]')
                    
        # Also let's change legend to not say 'Tasa' if it's count
        if panel.get('title', '').startswith('Conteo de Errores'):
            if panel['fieldConfig']['defaults'].get('unit') == 'reqps':
                panel['fieldConfig']['defaults']['unit'] = 'short'

with open('observability/grafana/provisioning/dashboards/medipass-dashboard.json', 'w', encoding='utf-8') as f:
    json.dump(dashboard, f, indent=2, ensure_ascii=False)
print('Dashboard updated successfully.')
