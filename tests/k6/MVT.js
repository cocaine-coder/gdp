import http from 'k6/http';
import { sleep } from 'k6';

export const options = {
    vus: 1000,
    duration: '30s',
};

export default function(){
    http.get('http://localhost:5000/api/v2/geo/mvt/test/table1/geom/id/13/6837/3354?columns=properties')
    sleep(1);
};