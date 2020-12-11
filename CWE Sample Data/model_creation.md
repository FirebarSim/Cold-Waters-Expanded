# Creating New Weapons

## Introduction

This is a brief introduction describing how to create simple weapon models in Blender to be used in Cold Waters. This Tutorial will cover how to make the UUM-125B Sea Lance, using a Mk-50 torpedo model that I have already created. All files are available on this Git Hub or are creatable from files on this Git Hub.

## Step 1 - Sizing

The first step is to get our model's sizing correct, I am going to start out by creating a *dimension box* this is a cube that matches the maximum dimensions of the subject model. We begin by creating a Cube by selecting: `Add > Mesh > Cube`

![](/CWE%20Sample%20Data/images/Screenshot%202020-12-10%20222504.png)

Then we toggle into *Edit Mode* by pressing `Tab` and select wireframe view by pressing `Shift+Z` this means we can select items we couldnt normall see in a view.

![](/CWE%20Sample%20Data/images/Screenshot%202020-12-10%20222553.png)

Using the window in the top right of the viewport we select `Global` for our transform reference (that is relative to the 0,0,0 global axis system, not the 0,0,0 local axis system). 

![](/CWE%20Sample%20Data/images/Screenshot%202020-12-10%20222625.png)

We now position the top and bottom of the cube at `z=21/2"` and `z=-21/2"` respectively this is because this missile fits in a 21 inch torpedo tube, Blender will do the maths of converting this to metres for us.

Then we do the same for the left and right edges, setting them to `x=+/- 21/2"`.

Finally we do the front, I set this to `y=-1.45` as I know this is the forward extent of this Mk-50 torpedo and the back I set to `y=6.25-1.45` again letting blender do the maths for me.

I now have a box the dimensions of the UUM-125B capsule.

![](/CWE%20Sample%20Data/images/Screenshot%202020-12-10%20222914.png)

## Step 2 - Simple Mesh by Extrusion

My next step is to create the capsule, I do this be creating a UV Sphere by selecting `Add > Mesh > UV Sphere`. This places a UV sphere at the 3d Cursor.

![](/CWE%20Sample%20Data/images/Screenshot%202020-12-10%20225843.png)

I then move the sphere along the y axis by pressing `g` and then `y` until it is close to the nose of the torpedo, I scale the shape down until it is a good match for the shape of the nose by pressing `s`. Then I `Tab` into edit mode and select the verticies that fall outside the dimension box.

![](/CWE%20Sample%20Data/images/Screenshot%202020-12-10%20230015.png)

The next bits come in quick sucession and give an idea of how quickly meshes can be built up in blender.

I delete the verticies I just selected by pressing `delete` and selecting `verticies`. 

Then I select **all** the verticies along the back edge of the remaining part of the sphere. 

Next I press `f` to create a new face from these verticies. 

Then I press `e` and then `y` to extrude this face along the y axis only. I drag the face almost all the way to the back of the dimension box and then fix it in place by clicking.

I then press `e` and `y` again to extrude, I move the face an arbitrary amount and fix it by clicking. Then in the window we used to set up the dimension box I set `y=6.25-1.45` and the face will exactly align with the dimension box.

The capsule has a slight taper at the end so i now press `s` to scale the last ring of verticies down.

Next I right click and select `Shade Smooth` and then in the Mesh Panel on the right I expan the normals section and turn on Auto Smooth to leave me with a mesh like below.

![](/CWE%20Sample%20Data/images/Screenshot%202020-12-10%20230259.png)

## Step 3 - Simple UV Mapping

The next step is getting prepared to put a texture on the model, via a technique called UV Mapping. This translates coordinates in 3d space to 2d Space to map a flat texture to a 3d model.

We start by marking in a seam around the nose cap of the capsule, we change our selection mode to *edges* and select all the edges where we extruded the first time. Then we right click and select `Mark Seam`.

![](/CWE%20Sample%20Data/images/Screenshot%202020-12-10%20230401.png)

I do the same along the top and bottom most long edges of the capsule and the two rings of verticies at the back. 

At the same time I decide to add another ring of verticies in the middle to make mapping easier. I do this by selecting all the edges in the long section of the capsule, then right click and select `subdivide`. I make this new ring of edges a seam too.

I then decide to smooth the transition between the dome and the tube of the capsule. I select the edge ring, then press `Ctrl+E` and select `Bevel Edges` I play with the number of edges and the amout until it looks nice. Then I remove the extra seam that crept in.

This leaves me with the below.

![](/CWE%20Sample%20Data/images/Screenshot%202020-12-11%20000535.png)

