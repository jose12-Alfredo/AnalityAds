"use client";

import { useParams } from "next/navigation";
import { DashboardEditor } from "@/components/dashboard-editor";

export default function DashboardEditorPage() {
  const params = useParams<{ dashboardId: string }>();
  return <DashboardEditor dashboardId={params.dashboardId} />;
}
