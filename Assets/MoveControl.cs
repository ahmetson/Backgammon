﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MoveControl : MonoBehaviour
{
    public StartSetup GameMaster;
    private List<GameObject> AvailablePieces = new List<GameObject>();
    private List<PieceContainer> AvailableSpots = new List<PieceContainer>();
    private List<PieceContainer> AvailableMoves = new List<PieceContainer>();
    public float y;
    private float LocationX, LocationY, LocationZ;
    private short piece, spot = 0;
    public int roll1, roll2 = 0;
    private Transform OldParent;
    private PieceContainer OldSpot;
    private PieceContainer CurrentSpot;
    public bool MyTurn, DiceRolled, Doubles, PlayerRolledDice, TurnOver, Winner = false;
    private bool ListsSet, InitialSet, PieceSelected, DiceViewed = false;
    public bool IsBlack;
    private JailControl Jail;
    private BaseControl Base;
    private bool ListenForSpace = true;

    //This is how to keep track of which rolls has been used
    public bool[] RollsUsed; 

    // Start is called before the first frame update
    void Start()
    {
        //Black goes first
        MyTurn = IsBlack;

        //Set jail and base variable based on color
        if (IsBlack)
        {
            Jail = GameMaster.BlackJailControl;
            Base = GameMaster.BlackBaseControl;
        }
        else
        {
            Jail = GameMaster.WhiteJailControl;
            Base = GameMaster.WhiteBaseControl;
        }
            
        PieceSelected = false;
        GetComponentInChildren<MeshRenderer>().enabled = false;

        //Declare length of 4 for Doubles
        RollsUsed = new bool[4];
    }

    // Update is called once per frame
    void Update()
    {
        if (!Winner)
        {
            if (MyTurn)
            {
                GameMaster.CurrentMover = this;

                if (TurnOver)
                {
                    //Press Space to hand off to other player
                    if (Input.GetKeyDown(KeyCode.Space))
                        GameMaster.SwitchTurns();
                }
                else
                {
                    //Roll dice if they haven't been yet
                    if (!DiceRolled)
                    {
                        if (PlayerRolledDice)
                            StartCoroutine(RollDice());
                        else if (Input.GetKeyDown(KeyCode.Space))
                            PlayerRolledDice = true;
                    }

                    //Set list of available spots and pieces
                    if (DiceViewed && !ListsSet)
                        SetAvailableLists();

                    //Set Location after lists have been set
                    if (ListsSet & !InitialSet)
                        SetInitialLocation();

                    //Enough time has passed for player to see result of dice
                    if (ListsSet & InitialSet)
                    {
                        //Select the next available piece or spot to move piece
                        if (Input.GetKeyDown(KeyCode.UpArrow))
                            SelectNext("Up");
                        if (Input.GetKeyDown(KeyCode.DownArrow))
                            SelectNext("Down");
                        //Select/Deselect piece or roll dice
                        if (Input.GetKeyDown(KeyCode.Space)&& ListenForSpace)
                        {
                            if(!PieceSelected)
                                StartCoroutine(SelectPiece());
                            else
                                StartCoroutine(SelectSpot());
                        }
                    }

                    //Cancel selection
                    if (Input.GetKeyDown(KeyCode.X))
                    {
                        if (PieceSelected)
                            CancelSelection();
                    }
                    transform.position = new Vector3(LocationX, LocationY, LocationZ);
                }
            }
            else
            {
                if (GameMaster.CurrentMover != this && !GameMaster.CurrentMover.MyTurn && !GameMaster.cam.flipping)
                    MyTurn = true;
                DiceRolled = DiceViewed = PlayerRolledDice = ListsSet = InitialSet = PieceSelected = false;
                GetComponentInChildren<MeshRenderer>().enabled = false;
            }
        }
    }

    bool PickingFromJail()
    {
        return (Jail.Pieces.Count > 0);
    }

    void CancelSelection()
    {
        PieceSelected = false;
        GetComponentInChildren<Renderer>().material.color = Color.red;
        AvailablePieces[piece].transform.parent = OldParent;
        OldSpot.GetComponent<SpotControl>().Changed = true;
        InitialSet = false;
    }

    void SetRollsUsed(int spotsmoved)
    {
        if (!Doubles)
        {
            //If the number of spots moved = one of the rolls -> That's the one used
            if (spotsmoved == roll1 && !RollsUsed[0])
                SetUsedRoll(0); 
            else if (spotsmoved == roll2 && !RollsUsed[1])
                SetUsedRoll(1);  
            else
            {
                //Spots moved is less than the rolls. Pick which ever one hasn't been used yet or the smallest.
                if (RollsUsed[0] && !RollsUsed[1])
                    SetUsedRoll(1);
                else if (!RollsUsed[0] && RollsUsed[1])
                    SetUsedRoll(0);
                else if (!RollsUsed[0] && !RollsUsed[1])
                {
                    if (roll1 < roll2)
                        SetUsedRoll(0);
                    else
                        SetUsedRoll(1);    
                }
            }    
        }
        else
        {
            //If doubles just set next one that hasn't been used
            int x = 0;
            while(RollsUsed[x])
                x++;
            SetUsedRoll(x);
        }
        ListsSet=false;
    }

    void SetUsedRoll(int x)
    {
        RollsUsed[x] = true;
        GameObject.Find(string.Concat("DiceRoll", x + 1)).GetComponent<DiceControl>().SetSprite(0);
    }

    void SetInitialLocation()
    {
        //Resets the location of mover after recalcing available spots
        spot = 0;
        CurrentSpot = AvailableSpots[spot];
        AvailablePieces = new List<GameObject>(CurrentSpot.Pieces);
        piece = 0;
        LocationX = AvailablePieces[piece].transform.position.x;
        LocationY = AvailablePieces[piece].transform.position.y + y;
        LocationZ = AvailablePieces[piece].transform.position.z;
        GetComponentInChildren<MeshRenderer>().enabled = true;
        InitialSet = true;
    }

    void SelectNext(string direction)
    {
        //If choosing piece to move
        if (!PieceSelected)
        {
            /*If there is a piece available in the direction chosen pick that one,
             *else move to next spot also in that direction and pick the first piece based on direction*/
            switch (direction)
            {
                case "Up":
                    if (piece < AvailablePieces.Count - 1)
                        piece++;
                    else
                    {
                        if (spot < AvailableSpots.Count - 1)
                            spot++;
                        else
                            spot = 0;
                        piece = 0;
                    }
                    break;
                case "Down":
                    if (piece > 0)
                        piece--;
                    else
                    {
                        if (spot > 0)
                            spot--;
                        else
                            spot = (short)(AvailableSpots.Count - 1);
                        piece = -1;
                    }
                    break;
            }
            CurrentSpot = AvailableSpots[spot];
            AvailablePieces = new List<GameObject>(CurrentSpot.Pieces);
            if (piece < 0) piece = (short)(AvailablePieces.Count - 1);
            LocationX = AvailablePieces[piece].transform.position.x;
            LocationY = AvailablePieces[piece].transform.position.y + y;
            LocationZ = AvailablePieces[piece].transform.position.z;
        }
        //choosing a spot to move piece to
        else
        {
            /*If there is a spot in the direction chosen move there
             * else loop to around list to first/last spot*/
            switch (direction)
            {
                case "Up":
                    if (spot < AvailableMoves.Count - 1)
                        spot++;
                    else
                        spot = 0;
                    break;
                case "Down":
                    if (spot > 0)
                        spot--;
                    else
                        spot = (short)(AvailableMoves.Count - 1);
                    break;
            }
            CurrentSpot = AvailableMoves[spot];
            LocationX = AvailableMoves[spot].transform.position.x;
            LocationY = AvailableMoves[spot].transform.position.y + y;
            LocationZ = AvailableMoves[spot].transform.position.z;
        }
    }

    IEnumerator RollDice() {
        //This will run on every update in the 1 second(s) it takes to set RiceRolled = true 
        DiceViewed = false;

        //Pick a random number between 1->6 for each dice
        roll1 = Random.Range(1, 6);
        roll2 = Random.Range(1, 6);

        //Set doubles bool
        Doubles = (roll1 == roll2);

        //Have gamemaster control displaying the role
        GameMaster.DisplayDice();

        //Refresh RollsUsed variables
        RollsUsed[0] = RollsUsed[1] = false;
        RollsUsed[2] = RollsUsed[3] = !Doubles;

        //Wait a little to simulate real dice roll
        yield return new WaitForSecondsRealtime(1f);
        DiceRolled = true;
        yield return new WaitForSecondsRealtime(.5f);
        DiceViewed = true;
    }

    IEnumerator SelectPiece()
    {
        //Set possible moves for piece selected
        OldSpot = CurrentSpot;
        AvailableMoves = OldSpot.ActualPossibleMoves;
        if (Base.BearingOff() && Base.ActualPossibleMoves.Contains(OldSpot))
            AvailableMoves.Add(Base);
        //Save old parent of piece and set new parent to mover so piece moves with it
        OldParent = AvailablePieces[piece].transform.parent;
        AvailablePieces[piece].transform.parent = transform;
        //Set mover color to yellow to indicate piece selected 
        GetComponentInChildren<Renderer>().material.color = Color.yellow;
        PieceSelected = true;
        //Move mover to first available spot
        yield return new WaitForSecondsRealtime(.1f);
        SelectNext("Up");
        yield return new WaitForSecondsRealtime(.1f);
        ListenForSpace = true;
    }

    IEnumerator SelectSpot()
    {
        //Calc Spots Moved (use abs because direction for white/black are opposite)
        int spotsmoved = Mathf.Abs(CurrentSpot.Position - OldSpot.Position);
        //Remove piece from old spot and add to new
        OldSpot.RemovePiece(AvailablePieces[piece]);
        CurrentSpot.AddPiece(AvailablePieces[piece]);
        //Drop piece from mover
        AvailablePieces[piece].transform.parent = OldParent;
        //Set mover color to red to indicate piece not selected
        GetComponentInChildren<Renderer>().material.color = Color.red;
        PieceSelected = false;
        //Update Rolls used based on spots moved
        SetRollsUsed(spotsmoved);
        yield return new WaitForSecondsRealtime(.1f);
        ListenForSpace = true;
    }

    void SetAvailableLists()
    {
        //Check if win
        if(Base.Pieces.Count == 15)
            Winner = true;
        else
        {
            //Clear old lists
            AvailableSpots.Clear();
            AvailablePieces.Clear();
            //If picking from jail there is no need to check all other spots
            if (PickingFromJail())
            {
                Jail.GetPossibleMoves(IsBlack, roll1, roll2);
                if (Jail.ActualPossibleMoves.Count > 0)
                    AvailableSpots.Add(Jail);
            }
            else
            {
                //Check each spot for possible moves
                foreach (PieceContainer s in GameMaster.AllSpots)
                {
                    s.GetPossibleMoves(IsBlack, roll1, roll2);
                    if (s.ActualPossibleMoves.Count > 0)
                    {
                        if (!AvailableSpots.Contains(s) && s.Pieces.Count > 0)
                            AvailableSpots.Add(s);
                    }
                }
                //If bearing off check the base for any possible moves
                if (Base.BearingOff())
                {
                    Base.GetPossibleMoves(IsBlack, roll1, roll2);
                    if (Base.ActualPossibleMoves.Count > 0)
                    {
                        foreach (PieceContainer s in Base.ActualPossibleMoves)
                        {
                            if (!AvailableSpots.Contains(s) && s.Pieces.Count > 0)
                                AvailableSpots.Add(s);
                        }
                    }
                }
            }
            //If there are no available spots left -> turn is over
            if (AvailableSpots.Count == 0)
                TurnOver = true;
            else
            {
                //Indicate that lists are set and the mover can move to initial spot
                ListsSet = true;
                InitialSet = false;
            }
        }        
    }
}
