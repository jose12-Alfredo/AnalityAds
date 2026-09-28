import { DashboardShareManager } from "@/components/dashboard-share-manager";
export default async function Page({params}:{params:Promise<{dashboardId:string}>}){const {dashboardId}=await params;return <DashboardShareManager dashboardId={dashboardId}/>;}
