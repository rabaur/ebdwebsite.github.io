---
layout: default
title: Exercise3
permalink: /exercise3/
---

[Exercise 1]({{ site.baseurl }}{% link exercises/exercise1.md %})\
[Exercise 2]({{ site.baseurl }}{% link exercises/exercise2.md %})\
[Exercise 3]({{ site.baseurl }}{% link exercises/exercise3.md %})\
[Final Project]({{ site.baseurl }}{% link final_project.md %})\
[Lecture Slides]({{ site.baseurl }}{% link ebd_lectureslides.md %})\
[Software]({{ site.baseurl }}{% link software.md %})

# Agent-based simulations to analyse design tradeoffs

**Objectives:** 
* The aim of this exercise is to apply agent-based simulation to understand how different layouts accommodate the same sequence of activities. For instance, in a hospital, there are many activities by different users at the same time. This exercise will help you measure how the SAME ACTIVITIES unfold across DIFFERENT BUILDING typologies and realise design tradeoffs.You can compare differences across the following measures: 
* Average walking distances (could reflect effort, and fatigue) 
* Average activity duration 
* Interactions between agents/collisions  (can reflect potential for social interactions or interruptions) 

**Submission date: 01.04.2022, 23:59**

**Submission files:** 
* A zip folder that includes your Unity project (which should include the scenes you used, the fbx models and the data files from the simulation).
* A report (in Miro) covering the different steps of the exercise (i.e., Activities table, Hypothesis, Results and visualizations, Interpretation) 
![image](/assets/images/exercise3/exc3.png)

**Submission Link (for zip folder):**  Drag and drop [here](https://polybox.ethz.ch/index.php/s/Iy3sePgdmvDPqpJ)

## Overview 
The exercise stages are:
* Decide which activities you would like to simulate (see example table in the lecture slides for Lecture#4) 
* Before you run the simulation of the activity sequence outlined in your table (for running the simulation please follow the tutorial below), hypothesise what would be the differences between the two typologies. For instance:
* Example hypothesis: *Since the racetrack typology has the nurse station at the centre, I expect that the average walking distance of nurses to be lower when compared to a courtyard typology in which the nurse station is situated at the corners of the layout, requiring more walking towards patient rooms.*  
**Note:** In your report you should have one hypothesis per measure (e.g., for social interactions/interruptions, walking distances, density).
* Using the data collected: compare average scores across metrics for relevant agents across both typologies (see exemplar table for one typology in the lecture slides). Compare activity heatmaps across both typologies. Assuming the same sequence of activities, what are the differences in the spatial distribution of activities in each typology?
* Interpret the results, which layout performed better? Can you identify design trade-offs? 

## Running the simulation in Unity3D

The UnityProject (to get accustomed with the setting) and tutorial videos:
* [UnityProject](ADDLINK) of the tutorial (you can either build upon this project or make one from scratch). 

* [Video 0](ADDLINK): Generating translucent (glass-like) materials.
* [Video 1](ADDLINK): Setting up a navigation mesh
* [Video 2](ADDLINK): Setting up points of interest
* [Video 3](ADDLINK): Analysis: Density-maps and comparison-maps
* [Video 4](ADDLINK): Investigating the data
* [Video 5](ADDLINK): Recording Footage
* [Lecture 4 recording](ADDLINK): The live demo from lecture#4


