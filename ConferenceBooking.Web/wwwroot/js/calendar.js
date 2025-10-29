// Simple daily calendar rendering and booking creation
(function(){
  const dayPicker = document.getElementById('dayPicker');
  const todayBtn = document.getElementById('todayBtn');
  const container = document.getElementById('calendarContainer');
  const formWrap = document.getElementById('createForm');
  const form = document.getElementById('bookingForm');
  const roomIdEl = document.getElementById('roomId');
  const titleEl = document.getElementById('title');
  const startTimeEl = document.getElementById('startTime');
  const durationEl = document.getElementById('duration');
  const errorBox = document.getElementById('errorBox');

  // Default day is today
  const toDateInputValue = (d) => d.toISOString().slice(0,10);
  const fromTimeStr = (str) => { const [h,m] = str.split(':').map(Number); return {h,m}; };

  const MIN_HOUR = 8;   // day view start hour
  const MAX_HOUR = 18;  // day view end hour
  const SLOT_MINUTES = 30; // grid granularity (30m)

  function init(){
    const now = new Date();
    dayPicker.value = toDateInputValue(now);
    todayBtn.addEventListener('click', ()=>{ dayPicker.value = toDateInputValue(new Date()); load(); });
    dayPicker.addEventListener('change', load);
    form.addEventListener('submit', onCreateSubmit);
    document.getElementById('cancelCreate').addEventListener('click', ()=>{ formWrap.style.display = 'none'; });
    load();
  }

  async function load(){
    formWrap.style.display = 'none';
    container.innerHTML = 'Loading...';
    const date = dayPicker.value;
    const res = await fetch(`/api/booking/getForDay?date=${date}`);
    const data = await res.json();
    renderDayGrid(data.rooms, data.bookings);
  }

  function renderDayGrid(rooms, bookings){
    // Normalize bookings to map by roomId
    const byRoom = new Map(rooms.map(r => [r.id, []]));
    for(const b of bookings){
      const arr = byRoom.get(b.roomId) || [];
      arr.push(b); byRoom.set(b.roomId, arr);
    }

    // Build table
    const table = document.createElement('table');
    table.style.borderCollapse = 'collapse';
    table.style.width = '100%';

    const thead = document.createElement('thead');
    const trh = document.createElement('tr');
    const thTime = document.createElement('th'); thTime.textContent = 'Time'; thTime.style.textAlign='left'; thTime.style.borderBottom='1px solid #ddd'; thTime.style.padding='6px';
    trh.appendChild(thTime);
    for(const r of rooms){
      const th = document.createElement('th');
      th.textContent = `${r.name} (${r.capacity})`;
      th.style.borderBottom='1px solid #ddd'; th.style.padding='6px'; th.style.textAlign='left';
      trh.appendChild(th);
    }
    thead.appendChild(trh);

    const tbody = document.createElement('tbody');
    const slots = [];
    for(let h=MIN_HOUR; h<MAX_HOUR; h++){
      for(let m=0; m<60; m+=SLOT_MINUTES){
        slots.push({h, m});
      }
    }

    const dateStr = dayPicker.value;

    for(const slot of slots){
      const tr = document.createElement('tr');
      const label = document.createElement('td');
      label.textContent = `${String(slot.h).padStart(2,'0')}:${String(slot.m).padStart(2,'0')}`;
      label.style.padding='6px'; label.style.borderBottom='1px solid #f0f0f0'; label.style.whiteSpace='nowrap';
      tr.appendChild(label);

      for(const r of rooms){
        const td = document.createElement('td');
        td.style.border='1px solid #f7f7f7';
        td.style.padding='4px';
        td.style.cursor='pointer';
        td.dataset.roomId = r.id;
        td.dataset.time = `${String(slot.h).padStart(2,'0')}:${String(slot.m).padStart(2,'0')}`;

        // Check if booking occupies this slot
        const cellStart = new Date(`${dateStr}T${td.dataset.time}:00`);
        const cellEnd = new Date(cellStart.getTime() + SLOT_MINUTES*60000);
        const bs = (byRoom.get(r.id)||[]).filter(b => new Date(b.endLocal) > cellStart && new Date(b.startLocal) < cellEnd);
        if(bs.length>0){
          td.style.background='#fdebd0';
          td.style.cursor='not-allowed';
          td.title = bs.map(b => `${b.title} (${new Date(b.startLocal).toLocaleTimeString([], {hour:'2-digit', minute:'2-digit'})}–${new Date(b.endLocal).toLocaleTimeString([], {hour:'2-digit', minute:'2-digit'})})`).join('\n');
          td.textContent = bs[0].title;
        } else {
          td.addEventListener('click', onFreeCellClick);
        }

        tr.appendChild(td);
      }

      tbody.appendChild(tr);
    }

    table.appendChild(thead);
    table.appendChild(tbody);
    container.innerHTML = '';
    container.appendChild(table);
  }

  function onFreeCellClick(e){
    const td = e.currentTarget;
    const time = td.dataset.time; // HH:mm
    const roomId = td.dataset.roomId;

    formWrap.style.display = '';
    roomIdEl.value = roomId;
    startTimeEl.value = time;
    durationEl.value = 30;
    titleEl.focus();
    errorBox.style.display='none';
    errorBox.textContent='';
    form.scrollIntoView({behavior:'smooth', block:'center'});
  }

  async function onCreateSubmit(ev){
    ev.preventDefault();
    errorBox.style.display='none';

    const date = dayPicker.value; // YYYY-MM-DD
    const {h, m} = fromTimeStr(startTimeEl.value);
    const startLocal = new Date(`${date}T${String(h).padStart(2,'0')}:${String(m).padStart(2,'0')}:00`);
    const duration = Math.max(15, Math.min(180, parseInt(durationEl.value,10)||30));
    const endLocal = new Date(startLocal.getTime() + duration*60000);

    const payload = {
      roomId: roomIdEl.value,
      startLocal: startLocal,
      endLocal: endLocal,
      title: titleEl.value.trim()
    };

    const res = await fetch('/api/booking/create', {
      method: 'POST', headers: {'Content-Type': 'application/json'}, body: JSON.stringify(payload)
    });

    if(!res.ok){
      const err = await res.json().catch(()=>({error:'Unknown error'}));
      errorBox.textContent = err.error || 'Unknown error';
      errorBox.style.display = '';
      return;
    }

    formWrap.style.display='none';
    titleEl.value='';
    await load();
  }

  document.addEventListener('DOMContentLoaded', init);
})();
