﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Character
{
    public class CharacterController : IUpdatable
    {
        CharacterModel _myModel;
        public CharacterController(CharacterModel model)
        {
            _myModel = model;
        }

        public void ArtificialUpdate()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                _myModel.TryJump();
            }
            if(Input.GetKeyDown(KeyCode.E))
            {
                _myModel.TryGrenade();
            }
            if (Input.GetKeyUp(KeyCode.E))
            {
                _myModel.ThrowGrenade();
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                _myModel.TryGrenadeDrunk();
            }
        }

        public void ArtificialFixedUpdate() { }

        public void ArtificialLateUpdate() { }
    }
}
