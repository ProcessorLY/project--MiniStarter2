import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AccessDenialComponent } from './core/components/access-denial/access-denial.component';
import { NotFoundComponent } from './core/components/not-found/not-found.component';
import { ServerErrorComponent } from './core/components/server-error/server-error.component';
import { AuthGuard } from './core/guards/auth.guard';
import { RoleGuard } from './core/guards/role.guard';
// import { AdminLayoutComponent } from './layouts/admin-layout/admin-layout.component';
// import { AuthLayoutComponent } from './layouts/auth-layout/auth-layout.component';
import { HomeLayoutComponent } from './layouts/home-layout/home-layout.component';

const routes: Routes = [
  {
    path: '', redirectTo: 'home', pathMatch: 'full'
  },
  // {
  //   path: '',
  //   component: AccountLayoutComponent,
  //   loadChildren: () => import('./modules/account/account.module').then(mod => mod.AuthModule),
  // },
  {
    path: 'home',
    canActivate: [AuthGuard],
    component: HomeLayoutComponent,
    loadChildren: () => import('./modules/home/home.module').then(mod => mod.HomeModule),
  },
  // {
  //   path: 'admin',
  //   canActivate: [AuthGuard, RoleGuard],
  //   component: AdminLayoutComponent,
  //   loadChildren: () => import('./modules/admin/admin.module').then(mod => mod.AdminModule),
  //   data: {
  //     allowedRoles: ['SuperAdmin']
  //   }
  // },
  {
    path: 'access-denial', component: AccessDenialComponent
  },
  {
    path: 'not-found', component: NotFoundComponent
  },
  {
    path: 'server-error', component: ServerErrorComponent
  },
  {
    path: '**', redirectTo: 'not-found', pathMatch: 'full'
  }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }