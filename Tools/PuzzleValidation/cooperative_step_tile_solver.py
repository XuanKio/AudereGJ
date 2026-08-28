"""Exact two-actor extension of the project StepTile solver: shared cards/red tiles, hold gates, arrival locking."""
import sys,json,importlib.util
from pathlib import Path
sys.stdout.reconfigure(encoding='utf-8')
f=Path(__file__).resolve().parents[2]/'.agents/skills/audere-puzzle-map-generator/scripts/step_tile_solver.py'
spec=importlib.util.spec_from_file_location('step_tile_solver',f);base=importlib.util.module_from_spec(spec);sys.modules[spec.name]=base;spec.loader.exec_module(base)

def solve(s):
 cells=set(map(tuple,s['cells']));red=set(map(tuple,s.get('one_use_cells',[])))
 starts=tuple(tuple(a['start']) for a in s['actors']);goals=tuple(tuple(a['goal']) for a in s['actors'])
 gates={tuple(g['cell']):tuple(g['hold']) for g in s.get('gates',[])}
 shared=tuple(map(tuple,s.get('cooperative_red_cells',[])))
 pieces=tuple(base.Piece(p['id'],tuple(map(tuple,p['path'])),i) for i,p in enumerate(s['pieces']))
 solutions=[];first=set();winning=set();traps=[];dead=0
 def visit(pos,remain,spent,shared_used,route):
  nonlocal dead
  if pos==goals:
   if not remain: solutions.append(route);winning.add(route[0])
   return
  if not remain:dead+=1;return
  for actor in range(2):
   if pos[actor]==goals[actor]:continue # Arrived actors fade and cannot receive another path.
   for pi,p in enumerate(remain):
    # Identical cards are interchangeable; do not count duplicate inventory permutations.
    if any(q.path==p.path and q.piece_id==p.piece_id for q in remain[:pi]):continue
    for m in base.placements(p,pos[actor],cells,frozenset(spent)):
     other=pos[1-actor]
     used=set(spent);shared_next=list(shared_used);valid=True
     for cell in m.path[1:]:
      if cell in used or (cell in gates and other!=gates[cell]):valid=False;break
      if cell in shared:
       ci=shared.index(cell);mask=shared_next[ci]
       if mask & (1<<actor) or (mask and other!=cell):valid=False;break
       shared_next[ci]|=1<<actor
      if cell in red:used.add(cell)
     if not valid:continue
     key=(actor,p.source_index,m.rotation,m.path)
     if not route:first.add(key)
     nxt=list(pos);nxt[actor]=m.end
     visit(tuple(nxt),remain[:pi]+remain[pi+1:],used,tuple(shared_next),route+(key,))
  if len(route)>0 and not any(pos[a]==goals[a] for a in range(2)):dead+=1
 visit(starts,pieces,set(starts)&red,tuple(sum(1<<a for a in range(2) if starts[a]==c) for c in shared),())
 def fmt(m):return dict(actor=s['actors'][m[0]]['id'],source_index=m[1],piece=s['pieces'][m[1]]['id'],rotation=m[2],path=m[3])
 out={'solved':bool(solutions),'solution_count':len(solutions),'distinct_first':len(first),'winning_first':len(winning),'traps':[fmt(m) for m in sorted(first-winning,key=repr)],'solutions':[[fmt(m) for m in r] for r in solutions[:10]],'all_solutions_use_both':all(len(set(m[0] for m in r))==2 for r in solutions),'first_actors':sorted(set(s['actors'][r[0][0]]['id'] for r in solutions))}
 # The existing solver also verifies each actor's projected route with its own two cards.
 if solutions:
  projected=[]
  for actor in range(2):
   r=[m for m in solutions[0] if m[0]==actor]
   q=dict(width=s['width'],height=s['height'],start=starts[actor],goal=goals[actor],cells=s['cells'],one_use_cells=s.get('one_use_cells',[]),pieces=[s['pieces'][m[1]] for m in r],require_all=True)
   projected.append(base.solve(q)['solution_count'])
  out['single_actor_projection_solutions']=projected
 return out

if __name__=='__main__':
 s=json.loads(Path(sys.argv[1]).read_text(encoding='utf-8-sig'));r=solve(s);print(json.dumps(r,ensure_ascii=False,indent=2));sys.exit(0 if r['solved'] else 1)
