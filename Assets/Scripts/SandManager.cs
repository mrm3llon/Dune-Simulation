using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SandManager : MonoBehaviour
{
    private float[] grid;
    private Vector3[] windgrid;
    private float[] shadowgrid;
    public float hopdistance=5.0f;
    public int gridsize;
    public float slabSize=3.0f;
    public float slabHeight=1.0f;
    public float maxSlopeAngle=30;
    public Vector3 windVelocity=Vector3.right;
    public Vector3 start;
    public Vector3 line;
    public float rico;
    public bool disableShadows;

    private int[] neighbours;
    
    void Awake()
    {
        //getCellsOnLine(Vector3.zero,Vector3.forward*10.0f);
        neighbours=new int[4]{1,gridsize,-1,-gridsize};
        grid=new float[gridsize*gridsize];
        windgrid=new Vector3[gridsize*gridsize];
        shadowgrid=new float[gridsize*gridsize];
        for(int i=0;i<gridsize;i++){
            for(int j=0;j<gridsize;j++){
                grid[i*gridsize+j]=Random.Range(10.0f,50.0f);//(i>10)?30.0f:0.0f;
                windgrid[i*gridsize+j]=windVelocity;
                
            }
        }
        
        
    }
    void Update()
    {
        Shadows();
        Erode();
        Flatten();
    }
    int posToCell(Vector3 position){
        Vector3 rescaled=position/slabSize;
        return (Mathf.RoundToInt(rescaled.x)+gridsize)%gridsize*gridsize+(Mathf.RoundToInt(rescaled.z)+gridsize)%gridsize;
    }
    Vector3 cellToPos(int idx){
        int j=idx%gridsize;
        int i=(idx-j)/gridsize;
        return new Vector3(i,0,j)*slabSize;
    }
    Vector3 ijToPos(Vector2Int ij){
        return new Vector3(ij.x,0,ij.y)*slabSize;
    }
    Vector2Int getIJ(int idx){
        int j=idx%gridsize;
        int i=(idx-j)/gridsize;
        return new Vector2Int(i,j);
    }
    void Erode(){
       
        for(int idx=0;idx<gridsize*gridsize;idx++){
            int indexToErode= Random.Range(0,gridsize*gridsize);
            
            if(shadowgrid[indexToErode]>grid[indexToErode]){
                continue;
            }
            Vector3 pos=cellToPos(indexToErode);
            Vector3 nextPos=pos+windVelocity*hopdistance;
            grid[indexToErode]-=1.0F;
            int nextIndex=posToCell(nextPos);
            //grid[nextIndex]+=1.0f;
            
            int steps=0;
            while(true){
                Vector3 delta=pos-nextPos;
                nextIndex=posToCell(nextPos);
                if(shadowgrid[nextIndex]>grid[nextIndex]){
                    
                    grid[nextIndex]+=1.0f;
                    break;
                }
                else if(Random.Range(0.0f,1.0f)<0.6f){
                    grid[nextIndex]+=1.0f;
                    break;
                }
                else if(steps==5){
                    grid[nextIndex]+=1.0f;
                    break;
                }
                

                steps++;
                nextPos+=windVelocity*hopdistance;
            }
            
        }
    }
    void Shadows(){
        for(int i=0;i<gridsize*gridsize;i++){
            shadowgrid[i]=grid[i];
        }
        if(disableShadows){
            return;
        }
        List<Vector2Int> shadowCells=getCellsOnLine(Vector3.zero,windVelocity.normalized*50.0f/rico);
        for(int j=0;j<gridsize*gridsize;j++){
            
            for(int k=0;k<shadowCells.Count;k++){
                
                Vector2Int shadowCoord=getIJ(j)+shadowCells[k];
                Vector3 delta=ijToPos(shadowCells[k]);
                int index=((shadowCoord.x+gridsize)%gridsize)*gridsize+(shadowCoord.y+gridsize)%gridsize;
                if(grid[j]-rico*delta.magnitude>shadowgrid[index]){
                    shadowgrid[index]=grid[j]-rico*delta.magnitude;
                }
                else{
                    break;
                }
                
            }
        }
    }
    List<Vector2Int> getCellsOnLine(Vector3 start,Vector3 line){
        
        
        Vector2Int startPos=getIJ(posToCell(start));
        
        List<float> verticalT=new List<float>();
        for(int i=1;i<Mathf.Abs(Mathf.Round((start.z+line.z)/slabSize)-Mathf.Round(start.z/slabSize))+1;i++){
            float h=start.z-Mathf.Round(start.z/slabSize)*slabSize;
            float t=((-0.5f+i)*slabSize*Mathf.Sign(line.z)-h)/line.z;
            verticalT.Add(t);
            
        }
        Gizmos.color=Color.blue;
        List<float> horizontalT=new List<float>();
        for(int i=1;i<Mathf.Abs(Mathf.Round((start.x+line.x)/slabSize)-Mathf.Round(start.x/slabSize))+1;i++){
            float h=start.x-Mathf.Round(start.x/slabSize)*slabSize;
            float t=((-0.5f+i)*slabSize*Mathf.Sign(line.x)-h)/line.x;
            horizontalT.Add(t);
            
        }
        List<Vector2Int> relCoordinates=new List<Vector2Int>();
        List<int> cells=new List<int>();
        Vector2Int tempPos=startPos;
        int cellIndex;
        int steps=verticalT.Count+horizontalT.Count;
        for(int idx=0;idx<steps;idx++){
            if(verticalT.Count==0){
                tempPos.x+=(int)Mathf.Sign(line.x);
                horizontalT.RemoveAt(0);

                cellIndex=tempPos.x*gridsize+tempPos.y;
                relCoordinates.Add(tempPos);
                cells.Add(cellIndex);
                continue;
            }
            if(horizontalT.Count==0){
                tempPos.y+=(int)Mathf.Sign(line.z);
                verticalT.RemoveAt(0);
                cellIndex=tempPos.x*gridsize+tempPos.y;
                relCoordinates.Add(tempPos);
                cells.Add(cellIndex);
                continue;
            }
            if(verticalT[0]<horizontalT[0]){
                tempPos.y+=(int)Mathf.Sign(line.z);
                verticalT.RemoveAt(0);
            }
            else{
                tempPos.x+=(int)Mathf.Sign(line.x);
                horizontalT.RemoveAt(0);
            }
            cellIndex=tempPos.x*gridsize+tempPos.y;
            relCoordinates.Add(tempPos);
            cells.Add(cellIndex);
            
        }
        return relCoordinates;

    
    }
    void Flatten(){
        for(int idx=0;idx<gridsize*gridsize;idx++){
            //Check whether this cell needs to flatten
            int steepest=-1;
            float diff=-float.MaxValue;
            for(int n=0;n<neighbours.Length;n++){
                if(grid[idx]-grid[(idx+neighbours[n]+gridsize*gridsize)%(gridsize*gridsize)]>diff){
                    steepest=(idx+neighbours[n]+gridsize*gridsize)%(gridsize*gridsize);
                    diff=grid[idx]-grid[(idx+neighbours[n]+gridsize*gridsize)%(gridsize*gridsize)];
                }
            }
            if(grid[idx]-grid[steepest]>2){
                grid[idx]-=1.0f;
                grid[steepest]+=1;
            }
            

        }
    }

    //Quick Visualization
    void OnDrawGizmos()
    {
        if(Application.isPlaying){
            
            for(int i=0;i<gridsize;i++){
                for(int j=0;j<gridsize;j++){
                    Gizmos.color=Color.Lerp(Color.white,Color.black,grid[i*gridsize+j]/30.0f);
                    if(grid[i*gridsize+j]<shadowgrid[i*gridsize+j]){
                        Gizmos.color=Color.black;
                    }
                    else{
                        Gizmos.color=Color.white;
                    }
                    Gizmos.DrawCube(new Vector3(i,0,j)*slabSize+Vector3.up*slabHeight*grid[i*gridsize+j],new Vector3(slabSize,slabHeight,slabSize)*0.9f);
                    Gizmos.DrawLine(new Vector3(i,0,j)*slabSize+Vector3.up*slabHeight*grid[i*gridsize+j],new Vector3(i,0,j)*slabSize+Vector3.up*slabHeight*grid[i*gridsize+j]+windVelocity);
                }
            }
            Gizmos.color=Color.black;
            for(int i=0;i<gridsize;i++){
                for(int j=0;j<gridsize;j++){
                    //Gizmos.DrawCube(new Vector3(i,0,j)*slabSize+Vector3.up*slabHeight*(shadowgrid[i*gridsize+j]-0.1f),new Vector3(slabSize,slabHeight,slabSize)*0.9f);
                    
                }
            }
            Gizmos.color=Color.red;
            for(int idx=0;idx<1;idx++){
                int indexToErode= 0;
                

                Gizmos.DrawCube(cellToPos(indexToErode)+Vector3.up*slabHeight*grid[indexToErode],new Vector3(slabSize,slabHeight,slabSize));
                Vector3 pos=cellToPos(indexToErode);
                Vector3 nextPos=pos+windVelocity*hopdistance;

                int nextIndex=posToCell(nextPos);
                Gizmos.DrawCube(cellToPos(nextIndex)+Vector3.up*slabHeight*grid[nextIndex],new Vector3(slabSize,slabHeight,slabSize));
                
                /*grid[indexToErode]-=1.0f;
                grid[nextIndex]+=1.0f;
                */
            
            
            }

            Gizmos.color=Color.black;
            Gizmos.DrawLine(start,start+line);
            Vector2Int startPos=getIJ(posToCell(start));
        
            List<float> verticalT=new List<float>();
            for(int i=1;i<Mathf.Abs(Mathf.Round((start.z+line.z)/slabSize)-Mathf.Round(start.z/slabSize))+1;i++){
                float h=start.z-Mathf.Round(start.z/slabSize)*slabSize;
                float t=((-0.5f+i)*slabSize*Mathf.Sign(line.z)-h)/line.z;
                verticalT.Add(t);
                Gizmos.DrawSphere(start+line*t,0.3f);
            }
            Gizmos.color=Color.blue;
            List<float> horizontalT=new List<float>();
            for(int i=1;i<Mathf.Abs(Mathf.Round((start.x+line.x)/slabSize)-Mathf.Round(start.x/slabSize))+1;i++){
                float h=start.x-Mathf.Round(start.x/slabSize)*slabSize;
                float t=((-0.5f+i)*slabSize*Mathf.Sign(line.x)-h)/line.x;
                horizontalT.Add(t);
                Gizmos.DrawSphere(start+line*t,0.3f);
            }

            List<Vector2Int> cells=new List<Vector2Int>();
            Vector2Int tempPos=startPos;
            int steps=verticalT.Count+horizontalT.Count;
            for(int idx=0;idx<steps;idx++){
                if(verticalT.Count==0){
                    tempPos.x=(tempPos.x+(int)Mathf.Sign(line.x)+gridsize)%gridsize;
                    horizontalT.RemoveAt(0);
                    cells.Add(tempPos);
                    continue;
                }
                if(horizontalT.Count==0){
                    tempPos.y=(tempPos.y+(int)Mathf.Sign(line.z)+gridsize)%gridsize;
                    verticalT.RemoveAt(0);
                    cells.Add(tempPos);
                    continue;
                }
                if(verticalT[0]<horizontalT[0]){
                    tempPos.y=(tempPos.y+(int)Mathf.Sign(line.z)+gridsize)%gridsize;
                    verticalT.RemoveAt(0);
                }
                else{
                    tempPos.x=(tempPos.x+(int)Mathf.Sign(line.x)+gridsize)%gridsize;
                    horizontalT.RemoveAt(0);
                }
                cells.Add(tempPos);
                
            }
            for(int i=0;i<cells.Count;i++){
                int cellIndex=cells[i].x*gridsize+cells[i].y;
                Gizmos.DrawCube(cellToPos(cellIndex)+Vector3.up*slabHeight*grid[cellIndex],new Vector3(slabSize,slabHeight,slabSize));
            }
            
        }
    }
}
